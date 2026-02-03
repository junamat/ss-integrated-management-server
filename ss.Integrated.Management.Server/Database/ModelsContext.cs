using Microsoft.EntityFrameworkCore;

namespace ss.Internal.Management.Server.AutoRef;

public class ModelsContext : DbContext
{
    public DbSet<Models.Match> Matches { get; set; }
    public DbSet<Models.RefereeInfo> Referees { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql("Host=localhost;Database=ss;Username=ss;Password=ss;");
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Models.Match>(entity =>
        {
            entity.OwnsOne(m => m.Round, builder =>
            {
                builder.ToJson(); 
            });
            
            entity.OwnsOne(m => m.TeamRed, builder =>
            {
                builder.ToJson();
            });
            
            entity.OwnsOne(m => m.TeamBlue, builder =>
            {
                builder.ToJson();
            });
            
            entity.OwnsOne(m => m.Referee, builder =>
            {
                builder.ToJson();
            });
        });
    }
}