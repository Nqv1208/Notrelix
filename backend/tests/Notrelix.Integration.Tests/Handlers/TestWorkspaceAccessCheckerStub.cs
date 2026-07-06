using Notrelix.Application.Common.Models;

namespace Notrelix.Integration.Tests.Handlers;

public sealed class TestWorkspaceAccessCheckerStub : IWorkspaceAccessChecker
{
    private readonly bool _succeed;

    public TestWorkspaceAccessCheckerStub(bool succeed) => _succeed = succeed;

    public Task<Result> EnsureWorkspaceExistsAsync(Guid workspaceId, CancellationToken ct) =>
        Task.FromResult(_succeed ? Result.Success() : Result.Failure("not found"));

    public Task<Result> EnsureWorkspaceIsActiveAsync(Guid workspaceId, CancellationToken ct) =>
        Task.FromResult(_succeed ? Result.Success() : Result.Failure("not found"));
}
