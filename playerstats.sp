#include <sourcemod>
#include <cstrike>
#include <adminmenu>

public Plugin myinfo = {
    name = "Player Statistics",
    author = "Your Name",
    description = "Displays player statistics including kills, deaths, assists, ping and IP",
    version = "1.0",
    url = ""
};

public void OnPluginStart()
{
    // Register command for root admins only
    RegAdminCmd("sm_playerstats", Command_PlayerStats, ADMFLAG_GENERIC, "Display player statistics (Root Admin only)");
}

public Action Command_PlayerStats(int client, int args)
{
    // Check if command is from console
    if (client == 0)
    {
        ReplyToCommand(client, "[PlayerStats] This command can only be used in-game by root admins.");
        return Plugin_Handled;
    }
    
    // Check if client is in game and has root admin
    if (!IsClientInGame(client))
    {
        ReplyToCommand(client, "[PlayerStats] You must be in-game to use this command.");
        return Plugin_Handled;
    }
    
    // Additional admin check
    if (!CheckCommandAccess(client, "sm_playerstats", ADMFLAG_ROOT))
    {
        ReplyToCommand(client, "[PlayerStats] You do not have access to this command. Root admin required.");
        return Plugin_Handled;
    }

    // Print header
    PrintToConsole(client, "Player Statistics (Admin View):");
    PrintToConsole(client, "Name | SteamID64 | IP Address | Kills | Deaths | Assists | Ping");
    PrintToConsole(client, "-------------------------------------------------------------------------");
    
    // Loop through all players
    for (int i = 1; i <= MaxClients; i++)
    {
        if (IsClientInGame(i) && !IsFakeClient(i))
        {
            char name[MAX_NAME_LENGTH];
            char steamID64[64];
            char ip[32];
            int kills = GetClientFrags(i);
            int deaths = GetClientDeaths(i);
            int assists = CS_GetClientAssists(i);
            int ping = GetClientLatency(i, NetFlow_Both) * 1000.0;
            
            GetClientName(i, name, sizeof(name));
            GetClientIP(i, ip, sizeof(ip));
            
            if (GetClientAuthId(i, AuthId_SteamID64, steamID64, sizeof(steamID64)))
            {
                PrintToConsole(client, "%s | %s | %s | %d | %d | %d | %d", 
                    name, steamID64, ip, kills, deaths, assists, ping);
            }
        }
    }
    
    PrintToConsole(client, "-------------------------------------------------------------------------");
    PrintToConsole(client, "Note: This information is confidential and for admin use only.");
    return Plugin_Handled;
} 