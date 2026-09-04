using Microsoft.EntityFrameworkCore;
using SusBaligiSiparis.Models;

namespace SusBaligiSiparis.Data;

// Ana SusBaligiTakip uygulamasıyla AYNI Postgres veritabanına bağlanır, ama şemayı yönetmez:
// bu context'te Database.Migrate()/EnsureCreated() ASLA çağrılmaz. Musteriler/TurKategorileri/
// Varyantlar sadece okunur; Siparisler/SiparisSatirlari bu uygulamanın yazdığı tablolardır ve
// şemaları ana uygulamanın AddSiparis migration'ı tarafından oluşturulur.
public class SiparisDbContext : DbContext
{
    public SiparisDbContext(DbContextOptions<SiparisDbContext> options) : base(options)
    {
    }

    public DbSet<Musteri> Musteriler => Set<Musteri>();
    public DbSet<TurKategorisi> TurKategorileri => Set<TurKategorisi>();
    public DbSet<Varyant> Varyantlar => Set<Varyant>();
    public DbSet<Siparis> Siparisler => Set<Siparis>();
    public DbSet<SiparisSatir> SiparisSatirlari => Set<SiparisSatir>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,4)");
        }

        builder.Entity<Musteri>().ToTable("Musteriler");
        builder.Entity<TurKategorisi>().ToTable("TurKategorileri");
        builder.Entity<Varyant>().ToTable("Varyantlar");
        builder.Entity<Siparis>().ToTable("Siparisler");
        builder.Entity<SiparisSatir>().ToTable("SiparisSatirlari");

        builder.Entity<Varyant>()
            .HasOne(v => v.TurKategorisi)
            .WithMany(k => k.Varyantlar)
            .HasForeignKey(v => v.TurKategorisiId);

        builder.Entity<SiparisSatir>()
            .HasOne(s => s.Siparis)
            .WithMany(s => s.Satirlar)
            .HasForeignKey(s => s.SiparisId);

        builder.Entity<SiparisSatir>()
            .HasOne(s => s.Varyant)
            .WithMany()
            .HasForeignKey(s => s.VaryantId);
    }
}
