using KloroFil.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KloroFil.Web.Data;

public class KloroFilDbContext(DbContextOptions<KloroFilDbContext> options)
    : IdentityDbContext<Utilisateur, IdentityRole, string>(options)
{
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Classement> Classements => Set<Classement>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<SourceVeille> SourcesVeille => Set<SourceVeille>();
    public DbSet<TexteReglementaire> Textes => Set<TexteReglementaire>();
    public DbSet<Exigence> Exigences => Set<Exigence>();
    public DbSet<AspectEnvironnemental> Aspects => Set<AspectEnvironnemental>();
    public DbSet<ObjectifEnvironnemental> Objectifs => Set<ObjectifEnvironnemental>();
    public DbSet<Audit> Audits => Set<Audit>();
    public DbSet<ActionQse> Actions => Set<ActionQse>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Site>().HasIndex(s => s.Nom).IsUnique();

        b.Entity<Classement>(e =>
        {
            e.HasOne(c => c.Site).WithMany(s => s.Classements).HasForeignKey(c => c.SiteId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(c => new { c.SiteId, c.Rubrique });
        });

        b.Entity<Prescription>(e =>
        {
            e.HasOne(p => p.Site).WithMany(s => s.Prescriptions).HasForeignKey(p => p.SiteId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => p.ProchaineEcheance);
        });

        b.Entity<SourceVeille>().HasIndex(s => s.Url).IsUnique();

        b.Entity<TexteReglementaire>(e =>
        {
            e.HasOne(t => t.SourceVeille).WithMany().HasForeignKey(t => t.SourceVeilleId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(t => t.Statut);
            e.HasIndex(t => t.Url);
        });

        b.Entity<Exigence>()
            .HasOne(x => x.Texte).WithMany(t => t.Exigences).HasForeignKey(x => x.TexteReglementaireId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<AspectEnvironnemental>()
            .HasOne(a => a.Site).WithMany().HasForeignKey(a => a.SiteId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<Audit>()
            .HasOne(a => a.Site).WithMany().HasForeignKey(a => a.SiteId).OnDelete(DeleteBehavior.SetNull);

        // SQL Server refuse les chemins de cascade multiples : les liens des actions sont détachés par l'application
        // (voir DetacherActionsAsync) avant toute suppression de l'élément d'origine.
        b.Entity<ActionQse>(e =>
        {
            e.HasOne(a => a.Texte).WithMany().HasForeignKey(a => a.TexteReglementaireId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(a => a.Exigence).WithMany().HasForeignKey(a => a.ExigenceId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(a => a.Prescription).WithMany().HasForeignKey(a => a.PrescriptionId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(a => a.Audit).WithMany().HasForeignKey(a => a.AuditId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasOne(a => a.Aspect).WithMany().HasForeignKey(a => a.AspectEnvironnementalId).OnDelete(DeleteBehavior.ClientSetNull);
            e.HasIndex(a => new { a.Statut, a.Echeance });
        });
    }

    /// <summary>Conserve les actions mais retire leurs liens vers les éléments sur le point d'être supprimés.</summary>
    public async Task DetacherActionsAsync(int? texteId = null, int? prescriptionsDuSite = null, int? prescriptionId = null,
        int? auditId = null, int? aspectId = null)
    {
        if (texteId is { } t)
        {
            await Actions.Where(a => a.TexteReglementaireId == t).ExecuteUpdateAsync(s => s.SetProperty(a => a.TexteReglementaireId, (int?)null));
            await Actions.Where(a => a.Exigence!.TexteReglementaireId == t).ExecuteUpdateAsync(s => s.SetProperty(a => a.ExigenceId, (int?)null));
        }
        if (prescriptionsDuSite is { } site)
            await Actions.Where(a => a.Prescription!.SiteId == site).ExecuteUpdateAsync(s => s.SetProperty(a => a.PrescriptionId, (int?)null));
        if (prescriptionId is { } p)
            await Actions.Where(a => a.PrescriptionId == p).ExecuteUpdateAsync(s => s.SetProperty(a => a.PrescriptionId, (int?)null));
        if (auditId is { } au)
            await Actions.Where(a => a.AuditId == au).ExecuteUpdateAsync(s => s.SetProperty(a => a.AuditId, (int?)null));
        if (aspectId is { } asp)
            await Actions.Where(a => a.AspectEnvironnementalId == asp).ExecuteUpdateAsync(s => s.SetProperty(a => a.AspectEnvironnementalId, (int?)null));
    }
}
