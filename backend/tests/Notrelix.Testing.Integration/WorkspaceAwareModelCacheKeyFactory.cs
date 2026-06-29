using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Testing.Integration;

/// <summary>
/// Custom EF Core model cache key factory that includes workspace context in the cache key.
///
/// Without this, EF Core caches the model by context type only. The migration context
/// builds the model with _currentWorkspace = null, producing a `false` filter for all
/// IWorkspaceScoped entities. All test contexts then reuse that poisoned model.
///
/// With this factory, each distinct (context type, workspace ID, isSystemContext) tuple
/// gets its own cached model. The migration context's model is never reused by test contexts.
/// </summary>
public sealed class WorkspaceAwareModelCacheKeyFactory : IModelCacheKeyFactory
{
    private static readonly FieldInfo CurrentWorkspaceField = typeof(ApplicationDbContext)
        .GetField("_currentWorkspace", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public object Create(DbContext context, bool designTime)
    {
        if (context is ApplicationDbContext appContext)
        {
            var workspace = CurrentWorkspaceField.GetValue(appContext) as ICurrentWorkspace;
            return (context.GetType(), designTime,
                    workspace?.WorkspaceId,
                    workspace?.IsSystemContext);
        }

        return (context.GetType(), designTime);
    }

    public object Create(DbContext context) => Create(context, designTime: false);
}
