//IP based tools:
//Auto admin
//Banning
//Logging?
//$Pref::Server::IpTools::Info[ip] = name TAB autoAdmin TAB autoSuperAdmin TAB BanReason TAB BanOverTime TAB BanOverYear;
//$Pref::Server::IpTools::NameToIP[name] = ip

//TODO: Tie this all into the auto admin and ban guis
function serverCmdSetAuto(%client,%name,%level)
{
    %client = findClientByName(%name);
    if(%client && %client.isHost())
    {
        %ip = getSafeVariableName(%client.getRawIp());
        %name = %client.getPlayerName();
        %info = $Pref::Server::IpTools::Info[%ip];
        switch(%level)
        {
            case 1:
                chatMessageAll("",'\c2%1 is now Admin (Auto)', %name);
                %info = setField(%info,1,1);
                %info = setField(%info,2,0);
            case 2:
                chatMessageAll("",'\c2%1 is now Super Admin (Auto)', %name);
                %info = setField(%info,1,0);
                %info = setField(%info,2,1);
            default:
                %info = setField(%info,1,0);
                %info = setField(%info,2,0);
        }
        $Pref::Server::IpTools::Info[getSafeVariableName(%ip)] = %info;
    }
        
}

package ipTools
{   
    function ipToolsIpBan(%name,%reason,%bantime)
    {
        %ip = $Pref::Server::IpTools::NameToIP[%name];

        %banOverYear = 0;
        %banOverTime = 0;
        if (%banTime == -1)
        {
            %banOverYear = -1;
            %banOverTime = -1;
        }
        else 
        {
            %currTime = getCurrentMinuteOfYear ();
            %banOverYear = getCurrentYear ();
            %banOverTime = %currTime + %banTime;
            if (%banOverTime > 525600)
            {
                %banOverYear += 1;
                %banOverTime -= 525600;
            }
        }

        setField($Pref::Server::IpTools::Info[%ip],3,%reason);
        setField($Pref::Server::IpTools::Info[%ip],4,%banOverTime);
        setField($Pref::Server::IpTools::Info[%ip],5,%banOverYear);
    }

    function ipToolsBanCheck(%client)
    {
        //TODO: check if ban is over
        %ip = getSafeVariableName(%client.getRawIp());
        %info = $Pref::Server::IpTools::Info[%ip];
        if (!%info)
        {
            return "";
        }

        %banReason = getField(%info,3);
        %banOverTime = getField(%info,4);
        %banOverYear = getField(%info,6);
        
        if (%banOverTime > getCurrentMinuteOfYear() || %banOverYear > getCurrentYear() || %banOverTime == -1)
        {
			echo ("  BLID " @ %blid @ " is banned, rejecting");
			%this.client.isBanReject = 1;
			return "CR_BANNED " @ %banReason;
        }
        return "";
    }

    function ipToolsautoAdminCheck(%client)
    {
        %ip = getSafeVariableName(%client.getRawIp());
        %info = $Pref::Server::IpTools::Info[%ip];
        %curName = %client.getPlayerName();

        if (!%info)
        {
            $Pref::Server::IpTools::NameToIP[%curName] = %ip;
            $Pref::Server::IpTools::Info[%ip] = %curName TAB 0 TAB 0 TAB "" TAB "";
        }

        %logName = getField(%info,0);
        %admin = getField(%info,1);
        %superAdmin = getField(%info,2);

        //to trick the auto admin message to appear we put their name in the corresponding list
        if (%admin)
        {
            $Pref::Server::AutoAdminList = %client.get_blid();
        }
        if (%superAdmin)
        {
            $Pref::Server::AutoSuperAdminList = %client.get_blid();
        }

        //TODO: print previous name and new name to admins in addition to the ip


        setField($Pref::Server::IpTools::Info[%ip],0,%curName);
    }

    function BanManagerSO::addBan(%this, %adminID, %victimID, %victimBL_ID, %reason, %banTime)
    {  
        if ($Pref::Server::IpTools::NameToIP[%victimBL_ID] || findClientByName(%victimBL_ID))
        {
            ipToolsIpBan(%victimBL_ID);
            return parent::addBan(%this, %adminID, %victimID, %victimBL_ID, %reason, %banTime);
        }
    }

    function GameConnection::autoAdminCheck(%client)
    {
        ipToolsAutoAdminCheck(%client);
        parent::autoAdminCheck(%client);
    }

    function GameConnection::onConnectRequest (%client, %netAddress, %LANname, %blid, %clanPrefix, %clanSuffix, %clientNonce)
    {
        %ipToolsBanned = ipToolsBanCheck(%client);
        %return = parent::onConnectRequest (%client, %netAddress, %LANname, %blid, %clanPrefix, %clanSuffix, %clientNonce);

        if(%ipToolsBanned)
        {
            return %ipToolsBanned;
        }
        return %return;
    }
};
deactivatePackage("ipTools");
activatePackage("ipTools");