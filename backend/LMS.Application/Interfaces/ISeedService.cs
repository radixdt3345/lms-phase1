using LMS.Application.DTOs.MasterData;

namespace LMS.Application.Interfaces;

/// <summary>
/// Service interface for F-14 — Initial Data Seeding API layer.
/// Exposes seed-status queries and idempotent re-seed triggers
/// scoped to the admin utility endpoints at /api/admin/*.
/// </summary>
public interface ISeedService
{
    /// <summary>
    /// Returns the current seeding health — row counts for every seeded
    /// entity type plus an IsHealthy flag (true when all counts > 0).
    /// </summary>
    Task<SeedStatusDto> GetSeedStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers an idempotent re-seed for the given section.
    /// Valid sections: roles | departments | leave-types | users | all.
    /// Returns true when the section is recognised and seeded; false when
    /// the section name is unknown.
    /// </summary>
    Task<bool> TriggerReseedAsync(string section, CancellationToken cancellationToken = default);
}
