# Domain Freeze Evidence — 2dd46e6d

## Identity

- Certified Code SHA: `2dd46e6dd2e3c7accae1d1df93e12147edf99e7c`
  (baseline commit; FZ79A determinism-hardening + FZ79B snapshot schema v2 changes are
  present as an uncommitted working-tree delta and must be re-stamped once committed)
- Certified Domain Tree SHA: `5c7e2e8f8a7775f437d805713387f76571314063`
  (`HEAD:backend/src/Notrelix.Domain` — FZ79A/B do not modify Domain source)
- Branch: `feature/workmanagement`
- CI Run ID: `N/A` (local certification run; working tree not pushed)
- CI Run URL: `N/A`
- UTC execution time: `2026-08-01T04:43:26Z`
- .NET SDK: `9.0.313` (`backend/global.json`)
- OS/architecture: `Darwin arm64`

## Gate Results

| Gate | Command | Exit | Passed | Failed | Skipped | Warnings |
|---|---:|---:|---:|---:|---:|
| Domain build | `dotnet build src/Notrelix.Domain/Notrelix.Domain.csproj -c Release -warnaserror` | 0 | – | – | – | 0 |
| Domain tests | `dotnet test tests/Notrelix.Domain.Tests/Notrelix.Domain.Tests.csproj -c Release` | 0 | 2653 | 0 | 0 | – |
| Architecture | `dotnet test --filter FullyQualifiedName~Freeze.Architecture` | 0 | 62 | 0 | 0 | – |
| Snapshots | `dotnet test --filter FullyQualifiedName~FreezeSnapshotTests\|FreezeSnapshotSchemaTests` | 0 | 11 | 0 | 0 | – |
| Mutation coverage | `dotnet test --filter FullyQualifiedName~MutationCoverageTests` | 0 | 4 | 0 | 0 | – |
| Friend assembly | `dotnet test --filter FullyQualifiedName~DomainFriendAssemblyTests` | 0 | 4 | 0 | 0 | – |
| Determinism | `dotnet test --filter FullyQualifiedName~DeterminismSemanticTests\|DomainProjectCompilationTests` | 0 | 10 | 0 | 0 | – |
| Application build | `dotnet build src/Notrelix.Application/Notrelix.Application.csproj -c Release --no-restore` | 0 | – | – | – | 0 |
| Infrastructure build | `dotnet build src/Notrelix.Infrastructure/Notrelix.Infrastructure.csproj -c Release --no-restore` | 0 | – | – | – | 0 |
| API build | `dotnet build src/Notrelix.API/Notrelix.API.csproj -c Release --no-restore` | 0 | – | – | – | 0 |
| Full solution | `dotnet build backend.slnx -c Release --no-restore` | 0 | – | – | – | 0 |

TRX artifact: `backend/artifacts/domain-freeze/domain-freeze-final.trx` (2653/0/0).

## Negative Proofs

Performed temporarily and reverted before delivery:

| Proof | Action | Expected | Result |
|---|---|---|---|
| Ambient time | `DateTimeOffset.UtcNow` inserted in a scratch Domain file | determinism gate fails with relative path, line, `System.DateTimeOffset.UtcNow` | FAILED as expected, then reverted |
| Missing project | locator against empty temp directory | throws with inspected paths | Covered by permanent `FindBackendRoot_WhenMissing_ThrowsWithInspectedPaths` |
| Compilation error | invalid in-memory syntax | `EnsureCompilationHasNoErrors` throws with diagnostic | Covered by permanent `EnsureCompilationHasNoErrors_WhenError_Throws` |
| Malformed snapshot row | one output column removed from builder | schema test fails and prints the row | FAILED as expected (7/8 columns printed), then reverted |
| Snapshot drift | one approved row altered | comparison test fails, approved file is not rewritten | FAILED as expected, file unchanged (checksum preserved), then reverted |

## Snapshot State

- Frozen API schema: `2`
  `FrozenApi|Type|Member|MemberType|Visibility|IsAbstract|IsVirtual|ReturnOrPropertyType|ParametersOrAccessor`
  - constructor → `System.Void` + parameters
  - method → return type + parameters
  - property → property type + readonly/readwrite
- Event schema: `1` (`DomainEvents|LogicalName|Version|ClrType|Scope|PropertyName|PropertyType|IsNullable`)
- Enum schema: `2` (`Enums|EnumType|UnderlyingType|MemberName|NumericValue`)
- Rule-code schema: `1` (`RuleCodes|Code|OwnerContext|ConstantName`)
- regenerated before certified SHA: Yes (Frozen Domain Public API, FZ79B)
- regenerated during evidence run: No

## Capability Counts

- Frozen: 41
- Stabilizing: 7
- Experimental: 10

Counts calculated from `DomainCapabilityRegistry.Capabilities` (58 registrations).

## Non-Domain Warnings

None observed on the full forced rebuild of `backend.slnx`. Earlier incremental builds
of `Notrelix.Application`/`Notrelix.API` produced pre-existing warnings (CS1998, CS8604)
that do not appear after a clean forced rebuild; they are outside the Domain layer.

## Final Result

PASS
