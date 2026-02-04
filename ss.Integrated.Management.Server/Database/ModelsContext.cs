using Microsoft.EntityFrameworkCore;

namespace ss.Internal.Management.Server.AutoRef;

public class ModelsContext : DbContext
{
    public DbSet<Models.Match> Matches { get; set; }
    public DbSet<Models.RefereeInfo> Referees { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql("Host=localhost;Database=ss;Username=ss;Password=ss;");
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.TeamInfo>(entity =>
        {
            entity.HasOne(t => t.OsuData)
                .WithMany()
                .HasForeignKey(t => t.OsuID);

            entity.Navigation(t => t.OsuData).AutoInclude();
        });
    }
}