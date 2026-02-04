using BanchoSharp;
using BanchoSharp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ss.Internal.Management.Server.AutoRef;

public partial class AutoRef
{
    private Models.Match? currentMatch;
    private readonly string matchId;
    private readonly string refDisplayName;
    private readonly Models.MatchType type;
    
    private IBanchoClient? client;
    private string? lobbyChannelName;
    
    private int[] matchScore = [0, 0];
    private bool auto = false;

    public AutoRef(string matchId, string refDisplayName, Models.MatchType type)
    {
        this.matchId = matchId;
        this.refDisplayName = refDisplayName;
        this.type = type;
    }

    public async Task StartAsync()
    {
        using (var db = new ModelsContext())
        {
            currentMatch = await db.Matches
                .Include(m => m.Round)
                .Include(m => m.TeamRed)
                .Include(m => m.TeamBlue)
                .Include(m => m.Referee)
                .FirstOrDefaultAsync(m => m.Id == matchId && m.Type == (int)type) ?? throw new Exception("Match no encontrado en DB");
        }

        await ConnectToBancho();
    }

    private async Task ConnectToBancho()
    {
        var config = new BanchoClientConfig(
            new IrcCredentials(currentMatch.Referee.DisplayName, currentMatch.Referee.IRC)
        );

        client = new BanchoClient(config);

        client.OnMessageReceived += message => 
        {
            _ = HandleIrcMessage(message);
        };

        client.OnAuthenticated += () =>
        {
            _ = client.MakeTournamentLobbyAsync($"{Program.TournamentName}: {currentMatch.TeamRed.DisplayName} vs {currentMatch.TeamBlue.DisplayName}", true);
        };

        await client.ConnectAsync();
    }

    private async Task HandleIrcMessage(IIrcMessage msg)
    {
        if (msg.Parameters.Count < 2) return;
        
        if (msg.Command != "PRIVMSG") return;
        
        string prefix = msg.Prefix.StartsWith(":") ? msg.Prefix[1..] : msg.Prefix;
        string senderNick = prefix.Contains('!') ? prefix.Split('!')[0] : prefix;

        string target = msg.Parameters[0];
        string content = msg.Parameters[1];
        
        if (senderNick == "BanchoBot" && content.Contains("Created the tournament lobby"))
        {
            var parts = content.Split('/');
            var idPart = parts.Last().Split(' ')[0];
            lobbyChannelName = $"#mp_{idPart}";

            await client.JoinChannelAsync(lobbyChannelName);
            await InitializeLobbySettings();
            return;
        }

        if (string.IsNullOrEmpty(lobbyChannelName) ||
            !target.Equals(lobbyChannelName, StringComparison.OrdinalIgnoreCase))
            return;
        
        if (senderNick == "BanchoBot")
        {
            if (content.Contains("Team Red wins"))
            {
                matchScore[0]++;
                await PrintScore();
            }
            else if (content.Contains("Team Blue wins"))
            {
                matchScore[1]++;
                await PrintScore();
            }
        }
        
        if (content.StartsWith(">") &&
            senderNick.Equals(currentMatch.Referee.DisplayName, StringComparison.OrdinalIgnoreCase))
        {
            await ExecuteAdminCommand(content[1..].Split(' '));
        }
    }

    private async Task InitializeLobbySettings()
    {
        await client.SendPrivateMessageAsync(lobbyChannelName, "!mp set 2 3 2");
        //TODO addrefs streamers
    }

    private async Task PrintScore()
    {
        string scoreMsg = $"{currentMatch.TeamRed.DisplayName} {matchScore[0]} -- {matchScore[1]} {currentMatch.TeamBlue.DisplayName}";
        await client.SendPrivateMessageAsync(lobbyChannelName, scoreMsg);
    }

    private async Task ExecuteAdminCommand(string[] args)
    {
        switch (args[0].ToLower())
        {
            case "auto":
                auto = args.Length > 1 && args[1] == "on";
                await client.SendPrivateMessageAsync(lobbyChannelName, $"Auto-Ref status: {(auto ? "ENABLED" : "DISABLED")}");
                break;
            case "close":
                await client.SendPrivateMessageAsync(lobbyChannelName, "!mp close");
                await client.DisconnectAsync();
                break;
            case "invite":
                await client.SendPrivateMessageAsync(lobbyChannelName, $"!mp invite {currentMatch.TeamRed.DisplayName}");
                await client.SendPrivateMessageAsync(lobbyChannelName, $"!mp invite {currentMatch.TeamBlue.DisplayName}");
                break;
        }
    }
}