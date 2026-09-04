namespace Notrelix.Application.Features.Accounts.Public;

using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Accounts.Public.Facts;

/// <summary>
/// Isolated, deliberately-violating architecture fixture (backend/tests/AGENTS.md
/// §33 — architecture fixture rule).
///
/// This type is declared logically ON the producer's own-Public surface, which is
/// exactly the smuggling scenario `ARCH-BC-005` / TAC-M3A guards against: a
/// producer slipping a persistence mechanism onto its approved Public semantic
/// surface under the cover of "own Public".
///
/// It is named WITHOUT mechanism words (no Repository/Store/Session/Provider/
/// Gateway/Cache/Queue) so `PublicSemanticContractArchitectureTests` can prove the
/// rejection is structural — the surface leaks a strong persistence primitive
/// (IQueryable / DbSet) — and NOT caused by a name substring.
///
/// Never used by production code and never referenced by any composition root.
/// </summary>
public interface IAccountReadWriteSurface
{
    /// <summary>Leaks an IQueryable over a tracked producer fact — strong
    /// persistence evidence, not a semantic read.</summary>
    IQueryable<AccountMembershipAdmissionFact> All();

    /// <summary>Leaks the EF DbSet entry point — strong persistence evidence.</summary>
    DbSet<AccountMembershipAdmissionFact> Set();
}
