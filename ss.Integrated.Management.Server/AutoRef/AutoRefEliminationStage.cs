using BanchoSharp;
using BanchoSharp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ss.Internal.Management.Server.AutoRef;

public partial class AutoRefEliminationStage : IAutoRef
{
    private Models.MatchRoom? currentMatch;
    private readonly string matchId;
    private readonly string refDisplayName;

    private IBanchoClient? client;
    private string? lobbyChannelName;

    private int[] matchScore = [0, 0];
    private bool auto = false;
    private bool joined = false;

    private TeamColor firstPick;
    private TeamColor firstBan;
    
    private MatchState state;

    private TaskCompletionSource<string>? chatResponseTcs;

    private readonly Action<string, string> msgCallback;

    public enum TeamColor
    {
        TeamBlue,
        TeamRed
    }

    public enum MatchState
    {
        Idle,
        WaitingForStart,
        Playing,
        MatchFinished,
        MatchOnHold,
    }

    public AutoRefEliminationStage(string matchId, string refDisplayName, Action<string, string> msgCallback)
    {
        this.matchId = matchId;
        this.refDisplayName = refDisplayName;
        this.msgCallback = msgCallback;
    }

    public async Task StartAsync()
    {
        await using (var db = new ModelsContext())
        {
            currentMatch = await db.MatchRooms.FirstAsync(m => m.Id == matchId) ?? throw new Exception("Match no encontrado en DB");
            currentMatch.Referee = await db.Referees.FirstAsync(r => r.DisplayName == refDisplayName) ?? throw new Exception("Referee no encontrado en DB");
        }

        await ConnectToBancho();
    }

    public Task StopAsync()
    {
        throw new NotImplementedException();
    }

    private async Task ConnectToBancho()
    {
        var config = new BanchoClientConfig(
            new IrcCredentials(currentMatch!.Referee.DisplayName, currentMatch.Referee.IRC)
        );

        client = new BanchoClient(config);

        client.OnMessageReceived += message =>
        {
            _ = HandleIrcMessage(message);
        };

        client.OnAuthenticated += () =>
        {
            _ = client.MakeTournamentLobbyAsync($"{Program.TournamentName}: jowjowosu vs methalox", true);
        };

        await client.ConnectAsync();
    }

    private async Task HandleIrcMessage(IIrcMessage msg)
    {
        string prefix = msg.Prefix.StartsWith(":") ? msg.Prefix[1..] : msg.Prefix;
        string senderNick = prefix.Contains('!') ? prefix.Split('!')[0] : prefix;

        //string target = msg.Parameters[0];
        string content = msg.Parameters[1];

        Console.WriteLine($"{senderNick}: {content}");

        if (joined) msgCallback(matchId, $"**[{senderNick}]** {content}");

        switch (senderNick)
        {
            case "BanchoBot" when content.Contains("Created the tournament match"):
                var parts = content.Split('/');
                var idPart = parts.Last().Split(' ')[0];
                lobbyChannelName = $"#mp_{idPart}";

                await client!.JoinChannelAsync(lobbyChannelName);
                await InitializeLobbySettings();
                joined = true;
                return;
            case "BanchoBot" when content.Contains("Closed the match"):
                await client!.DisconnectAsync();
                break;
            case "BanchoBot" when chatResponseTcs != null && SearchKeywords(content):
                chatResponseTcs.TrySetResult(content);
                chatResponseTcs = null;
                break;
        }

        if (senderNick == "BanchoBot") _ = TryStateChange(content);

        // REGIÓN DEDICADA AL !PANIC. ESTÁ DESACOPLADA DEL RESTO POR SER UN CASO DE EMERGENCIA
        // QUE NO DEBERÍA CAER EN NINGUNA OTRA SUBRUTINA

        if (content.Contains("!panic_over"))
        {
            await SendMessageBothWays("Going back to auto mode. Starting soon...");
            state = MatchState.WaitingForStart;
            await SendMessageBothWays("!mp timer 10");
        }
        else if (content.Contains("!panic"))
        {
            state = MatchState.MatchOnHold;
            await SendMessageBothWays("!mp aborttimer");

            await SendMessageBothWays(
                $"<@&{Environment.GetEnvironmentVariable("DISCORD_REFEREE_ROLE_ID")}>, {senderNick} has requested human intervention. Auto mode has been disabled, resume it with !panic_over");
        }

        if (content.StartsWith('>'))
        {
            await ExecuteAdminCommand(content[1..].Split(' '));
        }
    }

    public async Task SendMessageFromDiscord(string content)
    {
        await client!.SendPrivateMessageAsync(lobbyChannelName!, content);
    }

    private async Task InitializeLobbySettings()
    {
        await client!.SendPrivateMessageAsync(lobbyChannelName!, "!mp set 2 3 3");
        await client!.SendPrivateMessageAsync(lobbyChannelName!, "!mp invite " + currentMatch!.Referee.DisplayName);
    }

    private async Task PrintScore()
    {
        string scoreMsg = $"{currentMatch!.TeamRed.DisplayName} {matchScore[0]} -- {matchScore[1]} {currentMatch.TeamBlue.DisplayName}";
        await client!.SendPrivateMessageAsync(lobbyChannelName!, scoreMsg);
    }

    private async Task ExecuteAdminCommand(string[] args)
    {
        Console.WriteLine("admin command ejecutando");

        switch (args[0].ToLower())
        {
            case "close":
                await SendMessageBothWays("!mp close");
                break;
            case "invite":
                await SendMessageBothWays($"!mp invite {currentMatch!.TeamRed.DisplayName}");
                await SendMessageBothWays($"!mp invite {currentMatch!.TeamBlue.DisplayName}");
                break;
            case "start":
                await SendMessageBothWays($"Engaging autoreferee mode for Elimination Stage, Lobby {currentMatch!.Id}");
                // TODO: Do.
                break;
        }
    }

    private async Task SendMessageBothWays(string content)
    {
        await client!.SendPrivateMessageAsync(lobbyChannelName!, content);
        msgCallback(matchId, $"**[AUTO | {currentMatch!.Referee.DisplayName}]** {content}");
    }

    private async Task WaitForResponseAsync(string keyword)
    {
        chatResponseTcs = new TaskCompletionSource<string>();

        var ct = new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token;

        await using (ct.Register(() => chatResponseTcs.TrySetCanceled()))
        {
            await chatResponseTcs.Task;
        }
    }

    private bool SearchKeywords(string content)
    {
        bool found = content switch
        {
            var s when s.Contains("All players are ready") => true,
            var s when s.Contains("Changed beatmap") => true,
            var s when s.Contains("Enabled") => true,
            var s when s.Contains("Countdown finished") => true,
            _ => false
        };

        return found;
    }

    private async Task TryStateChange(string banchoMsg) // transiciones de estado
    {
        if (state == MatchState.Idle) return;

        if (state == MatchState.WaitingForStart)
        {
            if (banchoMsg.Contains("All players are ready") || banchoMsg.Contains("Countdown finished"))
            {
                await SendMessageBothWays("!mp start 10");
                state = MatchState.Playing;
            }
        }
        else if (state == MatchState.Playing)
        {
            if (banchoMsg.Contains("The match has finished"))
            {
                //TODO enseñar scores
                state = MatchState.Idle;
            }
        }
    }
}