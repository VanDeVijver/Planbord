using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Planbord.Core;

public class PlanbordDb(DbContextOptions<PlanbordDb> options) : IdentityDbContext<AppUser>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<Medewerker> Medewerkers => Set<Medewerker>();
    public DbSet<Project> Projecten => Set<Project>();
    public DbSet<Planning> Planningen => Set<Planning>();
    public DbSet<Afwezigheid> Afwezigheden => Set<Afwezigheid>();
    public DbSet<ProjectTaak> Taken => Set<ProjectTaak>();
    public DbSet<Bericht> Berichten => Set<Bericht>();
    public DbSet<Dagrapport> Dagrapporten => Set<Dagrapport>();
    public DbSet<DagrapportRegel> DagrapportRegels => Set<DagrapportRegel>();
    public DbSet<CategorieAandeel> CategorieAandelen => Set<CategorieAandeel>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<AppUser>().HasOne(u => u.Medewerker).WithMany().OnDelete(DeleteBehavior.SetNull);
        b.Entity<AppUser>().HasIndex(u => u.MedewerkerId).IsUnique();
        b.Entity<Planning>().HasIndex(p => new { p.Datum, p.MedewerkerId });
        b.Entity<Planning>().HasOne(p => p.Project).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<Bericht>().HasIndex(m => new { m.MedewerkerId, m.Datum }).IsUnique();
        b.Entity<Dagrapport>().HasIndex(r => new { r.MedewerkerId, r.Datum }).IsUnique();
        b.Entity<Dagrapport>().Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        b.Entity<DagrapportRegel>().Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        b.Entity<DagrapportRegel>().HasOne(r => r.Project).WithMany().OnDelete(DeleteBehavior.Restrict);
        b.Entity<CategorieAandeel>().Property(c => c.Categorie).ValueGeneratedNever();
        b.Entity<CategorieAandeel>().HasData(
            new CategorieAandeel { Categorie = 1, Procent = 35 }, new CategorieAandeel { Categorie = 2, Procent = 25 },
            new CategorieAandeel { Categorie = 3, Procent = 20 }, new CategorieAandeel { Categorie = 4, Procent = 10 },
            new CategorieAandeel { Categorie = 5, Procent = 10 });
    }
}
