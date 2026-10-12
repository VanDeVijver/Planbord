using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Planbord.Core;

public static class Rollen
{
    public const string Eigenaar = "EIGENAAR";
    public const string Planner = "PLANNER";
    public const string Medewerker = "MEDEWERKER";
    public static readonly string[] Alle = [Eigenaar, Planner, Medewerker];
}

public class AppUser : IdentityUser
{
    public int? MedewerkerId { get; set; }
    public Medewerker? Medewerker { get; set; }
}

public class Medewerker
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Naam { get; set; } = "";
    [MaxLength(30)] public string? Telefoon { get; set; }
    public bool Actief { get; set; } = true;
}

public class Project
{
    public int Id { get; set; }
    [Required, MaxLength(150)] public string Naam { get; set; } = "";
    [MaxLength(250)] public string? Adres { get; set; }
    public bool Gearchiveerd { get; set; }
    /// Planned period; drives the dashboard's length category and active/finished status.
    public DateOnly? Startdatum { get; set; }
    public DateOnly? Einddatum { get; set; }
    public List<ProjectTaak> Taken { get; set; } = [];
}

/// One planning row = one employee × one date × one project.
public class Planning
{
    public int Id { get; set; }
    public int MedewerkerId { get; set; }
    public Medewerker Medewerker { get; set; } = null!;
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public DateOnly Datum { get; set; }
    /// Set on create and on move; the earliest row of a day is the normal assignment.
    public DateTime ToegewezenOp { get; set; } = DateTime.UtcNow;
    public bool IsInterventie { get; set; }
    public bool Goedgekeurd { get; set; }
    public bool IsConflict => IsInterventie && !Goedgekeurd;
}

public class Afwezigheid
{
    public int Id { get; set; }
    public int MedewerkerId { get; set; }
    public Medewerker Medewerker { get; set; } = null!;
    public DateOnly Van { get; set; }
    public DateOnly Tot { get; set; }
    [Required, MaxLength(100)] public string Reden { get; set; } = "";
}

/// Shared across all scheduled days of a project, not copied per week.
public class ProjectTaak
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    [Required, MaxLength(300)] public string Omschrijving { get; set; } = "";
    public bool Klaar { get; set; }
    public DateTime? KlaarOp { get; set; }
    [MaxLength(100)] public string? KlaarDoor { get; set; }
}

/// Only the planner's extra text is stored; the summary is generated live.
public class Bericht
{
    public int Id { get; set; }
    public int MedewerkerId { get; set; }
    public Medewerker Medewerker { get; set; } = null!;
    public DateOnly Datum { get; set; }
    [MaxLength(2000)] public string? ExtraTekst { get; set; }
    public DateTime? GezienOp { get; set; }
}

/// Standard share of the 38-hour week a project takes, per length category (1–5).
public class CategorieAandeel
{
    [Key] public int Categorie { get; set; }
    [Range(0, 100)] public int Procent { get; set; }
}

public enum RapportStatus { Concept, Ingediend, Gesloten }
public enum RegelStatus { GEWERKT, NIET_GEWEEST, GEANNULEERD }

public class Dagrapport
{
    public int Id { get; set; }
    public int MedewerkerId { get; set; }
    public Medewerker Medewerker { get; set; } = null!;
    public DateOnly Datum { get; set; }
    public RapportStatus Status { get; set; }
    public TimeOnly? Start { get; set; }
    public TimeOnly? Einde { get; set; }
    public int PauzeMinuten { get; set; }
    public DateTime? IngediendOp { get; set; }
    public DateTime? GeslotenOp { get; set; }
    public bool VrijgegevenVoorCorrectie { get; set; }
    public List<DagrapportRegel> Regels { get; set; } = [];
}

public class DagrapportRegel
{
    public int Id { get; set; }
    public int DagrapportId { get; set; }
    public Dagrapport Dagrapport { get; set; } = null!;
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public RegelStatus? Status { get; set; }
    /// Task description for GEWERKT, reason otherwise.
    [MaxLength(2000)] public string? Omschrijving { get; set; }
    public int Minuten { get; set; }
}
