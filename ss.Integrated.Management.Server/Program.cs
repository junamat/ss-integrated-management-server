using DotNetEnv;
using ss.Internal.Management.Server.Discord;

namespace ss.Internal.Management.Server
{
	public static class Program
	{
		public const string TournamentName = "SS26";
		
		public static async Task Main(string[] args)
		{
			Console.WriteLine("hola server de jd");

			Env.Load();
			
			var manager = new DiscordManager(Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") ?? throw new InvalidOperationException());
			await manager.StartAsync();
			
			await Task.Delay(-1);
			Console.WriteLine("adios server de jd");
		}
	}
};

