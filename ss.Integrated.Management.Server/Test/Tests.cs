using Microsoft.EntityFrameworkCore;
using ss.Internal.Management.Server.AutoRef;

namespace ss.Integrated.Management.Server
{
    public partial class Tests
    {
        // EL SUPER TEST ESTE LO HA MONTAO UNA IA NO YO QUE ME DABA MUCHO PALO
        public static async Task TestFill()
        {
            await using var db = new ModelsContext();
            await db.Database.MigrateAsync(); // Asegura DB creada

            Console.WriteLine("--- Iniciando Seed ---");

            // 1. Crear Osu Users (Cache)
            var osuUser1 = new Models.OsuUser { Id = 727, DisplayName = "towny1" };
            var osuUser2 = new Models.OsuUser { Id = 12431, DisplayName = "A L E P H" };

            if (!db.Set<Models.OsuUser>().Any())
            {
                db.AddRange(osuUser1, osuUser2);
                await db.SaveChangesAsync();
            }

            // 2. Crear Usuarios de App (TeamInfo)
            var team1 = new Models.TeamInfo { OsuID = 727, DiscordID = "234547235647" };
            var team2 = new Models.TeamInfo { OsuID = 12431, DiscordID = "348756234" };

            if (!db.Set<Models.TeamInfo>().Any())
            {
                db.AddRange(team1, team2);
                await db.SaveChangesAsync();
                // Recargamos para asegurar que el AutoInclude traiga los nombres
                team1 = await db.Set<Models.TeamInfo>().FirstAsync(t => t.OsuID == 727);
                team2 = await db.Set<Models.TeamInfo>().FirstAsync(t => t.OsuID == 12431);
            }
            else
            {
                // Si ya existen, los cargamos
                team1 = await db.Set<Models.TeamInfo>().FirstAsync();
                team2 = await db.Set<Models.TeamInfo>().Skip(1).FirstAsync();
            }

            // 3. Crear Ronda Template (Tabla round)
            var roundTemplate = new Models.Round
            {
                Id = 1,
                DisplayName = "Finals",
                BestOf = 13,
                BanRounds = 1,
                Mode = Models.BansType.SpanishShowdown,
                MapPool = new List<Models.RoundBeatmap>
                {
                    new() { BeatmapID = 1453229, Slot = "NM1" },
                    new() { BeatmapID = 1453229, Slot = "HD1" },
                    new() { BeatmapID = 1453229, Slot = "HR1" },
                    new() { BeatmapID = 1453229, Slot = "DT1" },
                },
            };
            
            var roundTemplate2 = new Models.Round
            {
                Id = 2,
                DisplayName = "Group Stage",
                BestOf = 9,
                BanRounds = 1,
                Mode = Models.BansType.SpanishShowdown,
                MapPool = new List<Models.RoundBeatmap>
                {
                    new() { BeatmapID = 4305272, Slot = "NM1" },
                    new() { BeatmapID = 4032765, Slot = "NM2" },
                    new() { BeatmapID = 4187402, Slot = "NM3" },
                    new() { BeatmapID = 4745015, Slot = "NM4" },
                    new() { BeatmapID = 3832921, Slot = "NM5" },
                    new() { BeatmapID = 4128475, Slot = "HD1" },
                    new() { BeatmapID = 3597015, Slot = "HD2" },
                    new() { BeatmapID = 3689413, Slot = "HR1" },
                    new() { BeatmapID = 4478358, Slot = "HR2" },
                    new() { BeatmapID = 4253516, Slot = "DT1" },
                    new() { BeatmapID = 4741558, Slot = "DT2" },
                    new() { BeatmapID = 3155518, Slot = "DT3" },
                    new() { BeatmapID = 4365544, Slot = "TB1" },
                    
                },
            };

            if (!db.Set<Models.Round>().Any())
            {
                db.Add(roundTemplate);
                db.Add(roundTemplate2);
                await db.SaveChangesAsync();
            }

            // 4. CREAR EL MATCH
            var match = new Models.MatchRoom
            {
                Id = "6",
                StartTime = DateTime.UtcNow,
                TeamRedId = team1.Id,
                TeamBlueId = team2.Id,
                RoundId = roundTemplate.Id,
                RefereeId = null,
            };
            
            var match2 = new Models.MatchRoom
            {
                Id = "67",
                StartTime = DateTime.UtcNow,
                TeamRedId = team1.Id,
                TeamBlueId = team2.Id,
                RoundId = roundTemplate2.Id,
                RefereeId = null,
            };

            if (!await db.MatchRooms.AnyAsync(m => m.Id == match.Id))
            {
                db.MatchRooms.Add(match);
                db.MatchRooms.Add(match2);
                await db.SaveChangesAsync();
                Console.WriteLine("Match creado exitosamente.");
            }
            else
            {
                Console.WriteLine("El match ya existía.");
            }

            // LEER PARA COMPROBAR
            var savedMatch = await db.MatchRooms.FirstOrDefaultAsync(x => x.Id == "A5");
            if (savedMatch != null)
            {
                Console.WriteLine($"Leído de DB -> Red: {savedMatch.TeamRed.DisplayName} vs Blue: {savedMatch.TeamBlue.DisplayName}");
            }
        }
    }
};