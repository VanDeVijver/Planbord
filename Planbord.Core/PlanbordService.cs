using Microsoft.EntityFrameworkCore;

namespace Planbord.Core;

/// Business rules for planning and reports. Callers handle authorisation.
public class PlanbordService(PlanbordDb db)
{
    // ---------- Planning ----------

    async Task<string?> Blokkade(int medewerkerId, int projectId, DateOnly datum)
    {
        var m = await db.Medewerkers.FindAsync(medewerkerId);
        if (m is null || !m.Actief) return "Deze medewerker is niet actief.";
        var p = await db.Projecten.FindAsync(projectId);
        if (p is null || p.Gearchiveerd) return "Dit project is gearchiveerd.";
        if (await db.Afwezigheden.AnyAsync(a => a.MedewerkerId == medewerkerId && a.Van <= datum && a.Tot >= datum))
            return $"{m.Naam} is afwezig op {datum:dd/MM}.";
        return null;
    }

    async Task Herbereken(int medewerkerId, DateOnly datum)
    {
        var rijen = await db.Planningen.Where(p => p.MedewerkerId == medewerkerId && p.Datum == datum).ToListAsync();
        PlanningRegels.Herbereken(rijen);
    }

    public async Task<string?> Toewijzen(int medewerkerId, int projectId, DateOnly datum)
    {
        if (await Blokkade(medewerkerId, projectId, datum) is { } fout) return fout;
        db.Planningen.Add(new Planning { MedewerkerId = medewerkerId, ProjectId = projectId, Datum = datum });
        await db.SaveChangesAsync();
        await Herbereken(medewerkerId, datum);
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<(int Aangemaakt, List<string> Overgeslagen)> Plannen(IEnumerable<int> medewerkerIds, int projectId, IEnumerable<DateOnly> datums)
    {
        var aangemaakt = 0;
        var overgeslagen = new List<string>();
        foreach (var m in medewerkerIds)
            foreach (var d in datums)
            {
                if (await db.Planningen.AnyAsync(p => p.MedewerkerId == m && p.ProjectId == projectId && p.Datum == d)) continue;
                if (await Toewijzen(m, projectId, d) is { } fout) overgeslagen.Add(fout); else aangemaakt++;
            }
        return (aangemaakt, overgeslagen.Distinct().ToList());
    }

    public async Task<string?> Verplaats(int planningId, int medewerkerId, int projectId, DateOnly datum)
    {
        var p = await db.Planningen.FindAsync(planningId);
        if (p is null) return "Planning niet gevonden.";
        if (await Blokkade(medewerkerId, projectId, datum) is { } fout) return fout;
        var (oudM, oudD) = (p.MedewerkerId, p.Datum);
        p.MedewerkerId = medewerkerId; p.ProjectId = projectId; p.Datum = datum;
        p.ToegewezenOp = DateTime.UtcNow; p.Goedgekeurd = false;
        await db.SaveChangesAsync();
        await Herbereken(oudM, oudD);
        await Herbereken(medewerkerId, datum);
        await db.SaveChangesAsync();
        return null;
    }

    public async Task Verwijder(int planningId)
    {
        var p = await db.Planningen.FindAsync(planningId);
        if (p is null) return;
        db.Planningen.Remove(p);
        await db.SaveChangesAsync();
        await Herbereken(p.MedewerkerId, p.Datum);
        await db.SaveChangesAsync();
    }

    public async Task Goedkeuren(int planningId)
    {
        var p = await db.Planningen.FindAsync(planningId);
        if (p is { IsInterventie: true }) { p.Goedgekeurd = true; await db.SaveChangesAsync(); }
    }

    /// Records an absence and clears overlapping assignments; returns what was cleared.
    public async Task<List<string>> RegistreerAfwezigheid(int medewerkerId, DateOnly van, DateOnly tot, string reden)
    {
        db.Afwezigheden.Add(new Afwezigheid { MedewerkerId = medewerkerId, Van = van, Tot = tot, Reden = reden });
        var weg = await db.Planningen.Include(p => p.Project)
            .Where(p => p.MedewerkerId == medewerkerId && p.Datum >= van && p.Datum <= tot).ToListAsync();
        db.Planningen.RemoveRange(weg);
        await db.SaveChangesAsync();
        return weg.OrderBy(p => p.Datum).Select(p => $"{p.Datum:ddd dd/MM}: {p.Project.Naam}").ToList();
    }

    // ---------- Availability forecast ----------

    /// Share of the week per length category, index 0 = category 1.
    public async Task<int[]> Aandelen() =>
        (await db.CategorieAandelen.OrderBy(c => c.Categorie).Select(c => c.Procent).ToListAsync()).ToArray();

    /// Monthly forecast per active employee (or just one). A project counts for an employee once they are
    /// planned on it (past or future). A project past its end date on which the employee is still planned
    /// from today on keeps its load for them and is returned in Verlopen so the end date gets corrected.
    public async Task<Dictionary<int, PrognoseResultaat>> Prognoses(DateOnly vandaag, int maanden, int? medewerkerId = null)
    {
        var aandelen = await Aandelen();
        var paren = await db.Planningen.Where(p => medewerkerId == null || p.MedewerkerId == medewerkerId)
            .Select(p => new { p.MedewerkerId, p.ProjectId }).Distinct().ToListAsync();
        var projectIds = paren.Select(p => p.ProjectId).Distinct().ToList();
        var projecten = await db.Projecten.Where(p => projectIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var nogGepland = (await db.Planningen.Where(p => p.Datum >= vandaag && projectIds.Contains(p.ProjectId))
            .Select(p => new { p.MedewerkerId, p.ProjectId }).Distinct().ToListAsync())
            .Select(p => (p.MedewerkerId, p.ProjectId)).ToHashSet();
        var afwezig = await db.Afwezigheden.Where(a => (medewerkerId == null || a.MedewerkerId == medewerkerId) && a.Tot >= vandaag).ToListAsync();
        List<int> ids = medewerkerId is { } id ? [id] : await db.Medewerkers.Where(m => m.Actief).Select(m => m.Id).ToListAsync();

        return ids.ToDictionary(m => m, m =>
        {
            var eigen = paren.Where(p => p.MedewerkerId == m).Select(p => projecten[p.ProjectId]).ToList();
            var verlopen = eigen.Where(p => p.Einddatum < vandaag && nogGepland.Contains((m, p.Id))).ToList();
            var lasten = eigen.Select(p => new ProjectLast(p.Startdatum, p.Einddatum,
                aandelen[ProjectRegels.Categorie(p.Startdatum, p.Einddatum) - 1], verlopen.Contains(p))).ToList();
            var vrij = afwezig.Where(a => a.MedewerkerId == m).Select(a => (a.Van, a.Tot)).ToList();
            return new PrognoseResultaat(Prognose.PerMaand(vandaag, maanden, lasten, vrij), verlopen);
        });
    }

    // ---------- Reports ----------

    public async Task<List<Project>> GeplandeProjecten(int medewerkerId, DateOnly datum) =>
        (await db.Planningen.Include(p => p.Project).ThenInclude(p => p.Taken)
            .Where(p => p.MedewerkerId == medewerkerId && p.Datum == datum)
            .OrderBy(p => p.ToegewezenOp).ToListAsync())
        .Select(p => p.Project).DistinctBy(p => p.Id).ToList();

    public Task<Dagrapport?> Rapport(int medewerkerId, DateOnly datum) =>
        db.Dagrapporten.Include(r => r.Regels).FirstOrDefaultAsync(r => r.MedewerkerId == medewerkerId && r.Datum == datum);

    public async Task<List<string>> BewaarRapport(int medewerkerId, DateOnly datum, RapportInvoer invoer, bool indienen)
    {
        var rapport = await Rapport(medewerkerId, datum);
        if (rapport is { Status: not RapportStatus.Concept })
            return ["Dit rapport is ingediend en vergrendeld. Vraag de planner om het vrij te geven."];

        // Lines always follow the current planning; the client cannot add sites.
        var gepland = await GeplandeProjecten(medewerkerId, datum);
        invoer.Regels = gepland.Select(p =>
        {
            var r = invoer.Regels.FirstOrDefault(x => x.ProjectId == p.Id) ?? new RegelInvoer { ProjectId = p.Id };
            r.ProjectNaam = p.Naam;
            return r;
        }).ToList();

        var fouten = RapportRegels.Valideer(invoer, indienen);
        if (fouten.Count > 0) return fouten;

        if (rapport is null) db.Dagrapporten.Add(rapport = new Dagrapport { MedewerkerId = medewerkerId, Datum = datum });
        rapport.Start = invoer.Start; rapport.Einde = invoer.Einde; rapport.PauzeMinuten = invoer.PauzeMinuten;
        db.DagrapportRegels.RemoveRange(rapport.Regels);
        rapport.Regels = invoer.Regels.Select(r => new DagrapportRegel
        {
            ProjectId = r.ProjectId, Status = r.Status, Omschrijving = r.Omschrijving?.Trim(), Minuten = r.Minuten ?? 0,
        }).ToList();
        if (indienen)
        {
            rapport.Status = RapportStatus.Ingediend;
            rapport.IngediendOp = DateTime.UtcNow;
            rapport.VrijgegevenVoorCorrectie = false;
        }
        await db.SaveChangesAsync();
        return [];
    }

    public async Task<string?> SluitRapport(int rapportId)
    {
        var r = await db.Dagrapporten.FindAsync(rapportId);
        if (r is not { Status: RapportStatus.Ingediend }) return "Alleen ingediende rapporten kunnen gesloten worden.";
        r.Status = RapportStatus.Gesloten; r.GeslotenOp = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> GeefVrij(int rapportId)
    {
        var r = await db.Dagrapporten.FindAsync(rapportId);
        if (r is null || r.Status == RapportStatus.Concept) return "Dit rapport is niet vergrendeld.";
        r.Status = RapportStatus.Concept; r.GeslotenOp = null; r.VrijgegevenVoorCorrectie = true;
        await db.SaveChangesAsync();
        return null;
    }
}
