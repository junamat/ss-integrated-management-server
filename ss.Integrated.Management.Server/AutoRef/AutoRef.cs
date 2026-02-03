using System.Data;
using System.Text.RegularExpressions;
using BanchoSharp;
using BanchoSharp.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ss.Internal.Management.Server.AutoRef;

public partial class AutoRef
{
    private Models.Match currentMatch;
    private string matchId;
    private string refDisplayName;
    private Models.MatchType type;
    private IBanchoClient client;
    private IMultiplayerLobby lobby;
    
    public AutoRef(string matchId, string refDisplayName, MatchType type)
    {
        this.matchId = matchId;
        this.refDisplayName = refDisplayName;
        this.type = (Models.MatchType)type;
        
        currentMatch = GetCurrentMatch().Result;

        startMatchSequence();
    }

    private int startMatchSequence()
    {
        if(!TryRefereeAssignment().Result || currentMatch.Id == "Fallback" ) return -1;
        
        client = new BanchoClient(new BanchoClientConfig(new IrcCredentials(currentMatch.Referee.DisplayName, currentMatch.Referee.IRC)));
        client.ConnectAsync();

        client.OnAuthenticated += () =>
        {
            client.MakeTournamentLobbyAsync(Program.TournamentName, true);
        };

        client.BanchoBotEvents.OnTournamentLobbyCreated += (multiplayerLobby) =>
        {
            lobby = multiplayerLobby;
        };
        
        

        return 0;
    }


    private async Task<Models.Match> GetCurrentMatch()
    {
        using (var db = new ModelsContext())
        {
            return await db.Matches.FirstOrDefaultAsync(match => match.Id == matchId && match.Type == (int)type) ?? new Models.Match { Id = "Fallback" };
        }
    }
    
    private async Task<Boolean> TryRefereeAssignment()
    {
        bool found = false;
        
        using (var db = new ModelsContext())
        {
            var referee = await db.Referees.FirstOrDefaultAsync(reff => reff.DisplayName == refDisplayName) ?? new Models.RefereeInfo {  DisplayName = "Fallback" };
            if (referee.DisplayName != "Fallback") found = true;
            currentMatch.Referee = referee;
        }
        
        return found;
    }
}
