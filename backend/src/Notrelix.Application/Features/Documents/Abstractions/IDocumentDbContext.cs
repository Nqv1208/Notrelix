using Notrelix.Domain.Documents.ResourceLinks;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Documents.Versions;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Application.Features.Documents.Abstractions;

public interface IDocumentDbContext
{
    DbSet<Page> Pages { get; }
    DbSet<Block> Blocks { get; }
    DbSet<DocumentVersion> DocumentVersions { get; }
    DbSet<ResourceLink> ResourceLinks { get; }
    DbSet<PageTemplate> PageTemplates { get; }
}