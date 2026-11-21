using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Exports;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Exports;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.Chat;

/// <summary>
/// UC-CHT-H06 against PostgreSQL: the export worker's raw claim query matches the text the context stores for Queued, so a queued job is
/// claimed (it once matched the wire spelling and claimed nothing). The requester is not an active member, so the claimed job ends failed
/// with no-access; the point is that it left Queued. Needs Docker locally.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ChatExportClaimIntegrationTests
{
    private readonly OrgTreeTestHost _host;

    public ChatExportClaimIntegrationTests(PostgresTestContainer db) => _host = new OrgTreeTestHost(db);

    [Fact]
    public async Task AQueuedExportJobIsClaimedByTheWorker()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var job = ChatExportJob.Request(stack.AccountId, stack.WorkspaceId, stack.OwnerId, stack.OwnerId, null, null, _host.Now);
        await _host.WithSystemContextAsync(seed =>
        {
            seed.ChatExportJobs.Add(job);
            return Task.CompletedTask;
        });

        using var provider = _host.CreateProvider(stack.AccountId, stack.OwnerId, services =>
        {
            services.AddScoped<IChatDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            services.AddSingleton(new Mock<IStorageService>().Object);
            services.AddSingleton(new Mock<IWorkspaceMemberFacts>().Object);
            services.AddSingleton(new Mock<IChatSystemAudit>().Object);
            services.AddScoped<ChatExportBuilder>();
            services.AddScoped<ChatExportRunner>();
        });
        using (var scope = provider.CreateScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<ChatExportRunner>().RunNextAsync(CancellationToken.None));

        var status = await _host.ReadAsync(c => c.ChatExportJobs.AsNoTracking().Where(j => j.Id == job.Id).Select(j => j.Status).SingleAsync());
        Assert.NotEqual(ChatExportStatus.Queued, status);
    }
}
