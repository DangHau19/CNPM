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
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<VisitSession> VisitSessions => Set<VisitSession>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Ràng buộc CHECK của MySQL (xem DbConstraints) cho database tạo mới
        foreach (var table in DbConstraints.All.GroupBy(c => c.Table))
        {
            var entityType = table.Key switch
            {
                "Places" => typeof(Place),
                "PlaceTranslations" => typeof(PlaceTranslation),
                "Languages" => typeof(Language),
                "AdminUsers" => typeof(AdminUser),
                "PlayLogs" => typeof(PlayLog),
                "Visitors" => typeof(Visitor),
                "VisitSessions" => typeof(VisitSession),
                _ => throw new InvalidOperationException("Bảng không xác định: " + table.Key)
            };
            b.Entity(entityType).ToTable(t => { foreach (var c in table) t.HasCheckConstraint(c.Name, c.Sql); });
        }

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
            e.HasOne(x => x.Visitor).WithMany().HasForeignKey(x => x.VisitorId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Visitor>(e =>
        {
            e.HasIndex(x => x.ClientId).IsUnique();
            e.HasIndex(x => x.LastSeenAt);
            e.Property(x => x.ClientId).HasMaxLength(64).IsRequired();
            e.Property(x => x.DeviceLanguage).HasMaxLength(35).IsRequired();
        });

        b.Entity<VisitSession>(e =>
        {
            e.HasIndex(x => x.SessionKey).IsUnique();
            e.HasIndex(x => x.LastSeenAt);
            e.Property(x => x.SessionKey).HasMaxLength(64).IsRequired();
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.Property(x => x.UserAgent).HasMaxLength(400);
            e.Property(x => x.DeviceType).HasMaxLength(20).IsRequired();
            e.Property(x => x.Browser).HasMaxLength(40).IsRequired();
            e.Property(x => x.Os).HasMaxLength(40).IsRequired();
            e.Property(x => x.LanguageCode).HasMaxLength(10).IsRequired();
            e.HasOne(x => x.Visitor).WithMany(v => v.Sessions).HasForeignKey(x => x.VisitorId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
