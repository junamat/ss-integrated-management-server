using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace ss.Internal.Management.Server.AutoRef;

public class Models
{
    public class Match
    {
        public string Id { get; set; }
        public int Type { get; set; }
        public Round Round { get; set; }
        public TeamInfo TeamRed { get; set; }
        public TeamInfo TeamBlue { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsOver { get; set; }
        public RefereeInfo Referee { get; set; }
    }

    public class Round
    {
        public string DisplayName { get; set; }
        public int Bans { get; set; }
        public BansType Mode { get; set; }
        public int BestOf { get; set; }
    }

    public class TeamInfo
    {
        public string DisplayName { get; set; }
        public string DiscordID { get; set; }
        public string OsuID { get; set; }
    }
    
    public class RefereeInfo
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string DiscordID { get; set; }
        public string OsuID { get; set; }
        public string IRC { get; set; }
    }
    
    public enum BansType
    {
        SpanishShowdown = 0,
        Other = 1
    }

    public enum MatchType
    {
        EliminationStage = 0,
        QualifiersStage = 1,
    }
}