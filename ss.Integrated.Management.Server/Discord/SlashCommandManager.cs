using Discord;
using Discord.Interactions;
using System.Threading.Tasks;
using ss.Internal.Management.Server.AutoRef;

namespace ss.Internal.Management.Server.Discord;

public class MatchCommands : InteractionModuleBase<SocketInteractionContext>
{
    public DiscordManager Manager { get; set; }
    
    [RequireFromEnvId("DISCORD_REFEREE_ROLE_ID")]
    [SlashCommand("startref", "Inicia un nuevo match y crea su canal")]
    public async Task StartRefAsync(string matchId, string referee, int type)
    {
        await DeferAsync(ephemeral: false);

        bool created = await Manager.CreateMatchEnvironmentAsync(matchId, referee, Context.Guild);

        if (created)
            await FollowupAsync($"Match **{matchId}** iniciado correctamente.");
        else
            await FollowupAsync($"El Match ID **{matchId}** ya está en curso.", ephemeral: true);
    }
    
    [RequireFromEnvId("DISCORD_REFEREE_ROLE_ID")]
    [SlashCommand("endref", "Finaliza el match y borra el canal")]
    public async Task EndRefAsync(string matchId)
    {
        await RespondAsync($"Procesando cierre para **{matchId}**...");
        
        await Manager.EndMatchEnvironmentAsync(matchId, Context.Channel);
    }

    [RequireFromEnvId("DISCORD_REFEREE_ROLE_ID")]
    [SlashCommand("addref", "Añade un referee a la base de datos")]
    public async Task AddRefAsync(string nombre, int osuId, string ircPass)
    {
        ulong discordId = Context.User.Id;
        var model = new Models.RefereeInfo
        {
            DisplayName = nombre,
            OsuID = osuId,
            IRC = ircPass,
            DiscordID = (int)discordId
        };
        
        await Manager.AddRefereeToDbAsync(model);
        await RespondAsync($"Referee **{nombre}** añadido/actualizado en la base de datos.\n" +
                           $"- OsuID: {osuId}\n" +
                           $"- DiscordID: {discordId}", ephemeral: true);
    }
}