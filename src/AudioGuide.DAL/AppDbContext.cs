using AudioGuide.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AudioGuide.DAL;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<PlaceTranslation> PlaceTranslations => Set<PlaceTranslation>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<PlayLog> PlayLogs => Set<PlayLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Language>(e =>
        {
            e.HasKey(x => x.Code);
            e.Property(x => x.Code).HasMaxLength(10);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.NativeName).HasMaxLength(100).IsRequired();
            e.Property(x => x.SpeechLocale).HasMaxLength(20).IsRequired();
        });

        b.Entity<Place>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.Property(x => x.Address).HasMaxLength(300).IsRequired();
            e.Property(x => x.ImageUrl).HasMaxLength(500);
        });

        b.Entity<PlaceTranslation>(e =>
        {
            e.HasIndex(x => new { x.PlaceId, x.LanguageCode }).IsUnique();
            e.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.NarrationText).HasColumnType("text").IsRequired();
            e.Property(x => x.AudioUrl).HasMaxLength(500);
            e.HasOne(x => x.Place).WithMany(p => p.Translations)
                .HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Language).WithMany()
                .HasForeignKey(x => x.LanguageCode).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<AdminUser>(e =>
        {
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(50).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        });

        b.Entity<PlayLog>(e =>
        {
            e.HasIndex(x => x.PlayedAt);
            e.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.TriggerType).HasMaxLength(10).IsRequired();
            e.HasOne(x => x.Place).WithMany().HasForeignKey(x => x.PlaceId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
