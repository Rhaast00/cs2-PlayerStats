using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text;
using System.Net;

namespace PlayerStats;

[MinimumApiVersion(100)]
public class PlayerStats : BasePlugin
{
    private Dictionary<int, DateTime> _playerConnectTimes = new();

    public override string ModuleName => "PlayerStats";
    public override string ModuleVersion => "1.0";
    public override string ModuleAuthor => "rhasst";
    public override string ModuleDescription => "Displays player statistics including kills, deaths, assists, ping, IP and playtime";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        
        AddCommand("css_playerstats", "Display player statistics", CommandPlayerStats);
    }

    private HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player != null && player.IsValid && !player.IsBot && player.UserId.HasValue)
        {
            _playerConnectTimes[player.UserId.Value] = DateTime.Now;
        }
        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player != null && player.IsValid && player.UserId.HasValue)
        {
            _playerConnectTimes.Remove(player.UserId.Value);
        }
        return HookResult.Continue;
    }

    private string FormatPlayTime(TimeSpan playTime)
    {
        return $"{playTime.Hours:D2}:{playTime.Minutes:D2}:{playTime.Seconds:D2}";
    }

    private string GetPlayerIP(CCSPlayerController player)
    {
        try
        {
            var ipAddress = player.IpAddress;
            if (string.IsNullOrEmpty(ipAddress)) return "Unknown";
            
            // Port numarasını kaldır
            var portIndex = ipAddress.LastIndexOf(':');
            if (portIndex != -1)
            {
                ipAddress = ipAddress.Substring(0, portIndex);
            }
            
            return ipAddress;
        }
        catch
        {
            return "Unknown";
        }
    }

    private void CommandPlayerStats(CCSPlayerController? player, CommandInfo command)
    {
        // Yetki kontrolü
        if (player != null && !AdminManager.PlayerHasPermissions(player, "@css/root"))
        {
            player.PrintToChat(" \x02[PlayerStats]\x01 Bu komutu kullanmak için yetkiniz yok!");
            return;
        }

        // Konsol çıktısı için format
        var output = new StringBuilder();
        output.AppendLine("# | Name | SteamID | IP Address | Kills | Deaths | Assists | Ping | Time");
        output.AppendLine("----------------------------------------------------------------------------");

        var playerList = Utilities.GetPlayers()
            .Where(p => p.IsValid && !p.IsBot && p.UserId.HasValue)
            .OrderBy(p => p.UserId.Value);

        foreach (var currentPlayer in playerList)
        {
            if (!currentPlayer.UserId.HasValue) continue;

            var playTime = _playerConnectTimes.ContainsKey(currentPlayer.UserId.Value)
                ? DateTime.Now - _playerConnectTimes[currentPlayer.UserId.Value]
                : TimeSpan.Zero;

            var kills = currentPlayer.ActionTrackingServices?.MatchStats.Kills ?? 0;
            var deaths = currentPlayer.ActionTrackingServices?.MatchStats.Deaths ?? 0;
            var assists = currentPlayer.ActionTrackingServices?.MatchStats.Assists ?? 0;
            var ping = currentPlayer.Ping;
            var formattedTime = FormatPlayTime(playTime);
            var ipAddress = GetPlayerIP(currentPlayer);

            output.AppendLine($"{currentPlayer.UserId.Value,2} | " +
                            $"{currentPlayer.PlayerName,-20} | " +
                            $"{currentPlayer.SteamID,-17} | " +
                            $"{ipAddress,-15} | " +
                            $"{kills,5} | " +
                            $"{deaths,6} | " +
                            $"{assists,7} | " +
                            $"{ping,4} | " +
                            $"{formattedTime}");
        }

        output.AppendLine("----------------------------------------------------------------------------");
        output.AppendLine($"Total players: {playerList.Count()}");
        output.AppendLine("Note: This information is confidential and for admin use only.");

        // Çıktıyı gönder
        if (player == null)
        {
            Server.PrintToConsole(output.ToString());
        }
        else
        {
            player.PrintToConsole(output.ToString());
        }
    }
}