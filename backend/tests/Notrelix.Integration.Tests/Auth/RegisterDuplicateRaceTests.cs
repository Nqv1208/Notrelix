using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Security.Auth;
using Notrelix.Application.Features.Accounts.Provisioning;
using Notrelix.Application.Features.Identity.Registration.Commands.Register;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Users;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Auth;

[Collection("Database")]
public class RegisterDuplicateRaceTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public RegisterDuplicateRaceTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Handle_WhenTwoConcurrentRegistrationsUseSameEmail_OneSucceedsAndTheOtherMapsToConflict()
    {
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed-password");

        await using var firstContext = _db.CreateContext();
        await using var secondContext = _db.CreateContext();
        var first = CreateHandler(firstContext, passwordHasher.Object, dateTimeProvider.Object);
        var second = CreateHandler(secondContext, passwordHasher.Object, dateTimeProvider.Object);

        var command = new RegisterCommand
        {
            Email = "race@example.com",
            Password = "Password1!",
            Name = "Race"
        };

        await Task.WhenAll(
            first.Handle(command, CancellationToken.None),
            second.Handle(command, CancellationToken.None));

        var uniqueViolation = await SaveAndCaptureUniqueViolation(firstContext, secondContext);

        uniqueViolation.Should().NotBeNull(
            "the unique email index must reject the second concurrent registration");

        var mapping = new ExceptionMappingBehavior<RegisterCommand, Result>(
            NullLogger<ExceptionMappingBehavior<RegisterCommand, Result>>.Instance,
            new SystemExecutionContextReader());

        var act = async () => await mapping.Handle(
            command,
            (_) => throw uniqueViolation!,
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    private static async Task<DbUpdateException?> SaveAndCaptureUniqueViolation(
        ApplicationDbContext firstContext,
        ApplicationDbContext secondContext)
    {
        foreach (var context in new[] { firstContext, secondContext })
        {
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (
                (ex.InnerException?.Message ?? ex.Message).Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || (ex.InnerException?.Message ?? ex.Message).Contains("23505"))
            {
                return ex;
            }
        }

        return null;
    }

    private static RegisterCommandHandler CreateHandler(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider)
    {
        var jwtService = new Mock<IJwtService>();
        jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        jwtService.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        var sessionIssuer = new AuthSessionIssuer(jwtService.Object, context, dateTimeProvider);
        var integrationEventCollector = new Mock<IIntegrationEventCollector>();

        return new RegisterCommandHandler(
            context,
            new AccountProvisioningService(context, new AccessGrantProjectionService(context)),
            passwordHasher,
            sessionIssuer,
            dateTimeProvider,
            integrationEventCollector.Object);
    }

    private sealed class SystemExecutionContextReader : IExecutionContextReader
    {
        public Guid? UserId => null;
        public string? Email => null;
        public string? Name => null;
        public bool IsAuthenticated => false;
        public Guid? AccountId => null;
        public Guid? WorkspaceId => null;
        public bool IsSystemContext => true;
        public Guid CorrelationId => Guid.NewGuid();
        public Guid? CausationId => null;
        public bool IsResolved => true;

        public Guid RequireUserId() => throw new InvalidOperationException();
        public Guid RequireAccountId() => throw new InvalidOperationException();
        public Guid RequireWorkspaceId() => throw new InvalidOperationException();
    }
}
