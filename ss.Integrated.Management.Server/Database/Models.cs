using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

    [Table("round")]
    public class Round
    {
        [Column("id")]
        public int Id { get; set; }
        
        [Column("display_name")]
        public string DisplayName { get; set; }
        
        [Column("bans")]
        public int Bans { get; set; }
        
        [Column("ban_mode")]
        public BansType Mode { get; set; }
        
        [Column("best_of")]
        public int BestOf { get; set; }
        
        //[Column("mappool")]
        //public List<RoundBeatmap> MapPool { get; set; }
    }

    [Table("user")]
    public class TeamInfo
    {
        [Column("id")]
        public string Id { get; set; }
     
        [Key]
        [Column("osu_id")]
        public int OsuID { get; set; }
        
        [Column("discord_id")]
        public string DiscordID { get; set; }
        
        [ForeignKey("OsuID")]
        public virtual OsuUser OsuData { get; set; }

        public string DisplayName => OsuData.DisplayName ?? "Desconocido";
    }

    [Table("osu_user")]
    public class OsuUser
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("username")]
        public string DisplayName { get; set; }
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
        Other = 1,
    }

    public enum MatchType
    {
        EliminationStage = 0,
        QualifiersStage = 1,
    }
}