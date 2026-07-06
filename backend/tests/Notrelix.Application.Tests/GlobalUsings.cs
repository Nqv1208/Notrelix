global using MediatR;
global using Microsoft.Extensions.Logging;
global using Xunit;
global using FluentAssertions;
global using Moq;
global using Notrelix.Domain.WorkManagement.Boards.Events;
global using Notrelix.Domain.WorkManagement.Items.Events;

// Application Common (capability folders)
global using Notrelix.Application.Common.Context;
global using Notrelix.Application.Common.CQRS;
global using Notrelix.Application.Common.Data;
global using Notrelix.Application.Common.Data.Rls;
global using Notrelix.Application.Common.Events;
global using Notrelix.Application.Common.Exceptions;
global using Notrelix.Application.Common.Idempotency;
global using Notrelix.Application.Common.PostCommit;
global using Notrelix.Application.Common.Security;
global using Notrelix.Application.Common.Behaviors;
global using Notrelix.Application.Common.Caching;
global using Notrelix.Application.Common.CQRS.Caching;
global using Notrelix.Application.Common.Entitlements;
global using Notrelix.Application.Common.CQRS.Security;
global using Notrelix.Application.Common.Tenancy;
global using Notrelix.Domain.SharedKernel;
global using Notrelix.Domain.Governance.Permissions;
