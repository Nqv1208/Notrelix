global using MediatR;
global using Microsoft.Extensions.Logging;
global using Xunit;
global using FluentAssertions;
global using Moq;
global using Notrelix.Domain.WorkManagement.Boards.Events;
global using Notrelix.Domain.WorkManagement.Items.Events;

// Application Common (capability folders)
global using Notrelix.Application.Common.Context;
global using Notrelix.Application.Common.Requests;
global using Notrelix.Application.Common.Data;
global using Notrelix.Application.Common.Events;
global using Notrelix.Application.Common.Exceptions;
global using Notrelix.Application.Common.Idempotency;
global using Notrelix.Application.Common.PostCommit;
global using Notrelix.Application.Common.Security;
global using Notrelix.Application.Common.Behaviors;
global using Notrelix.Application.Common.Caching;
global using Notrelix.Application.Common.Requests.Scoping;
global using Notrelix.Application.Common.Requests.Execution;
global using Notrelix.Application.Common.Entitlements;
global using Notrelix.Application.Common.Requests.Security;
global using Notrelix.Application.Common.Tenancy;
global using Notrelix.Application.Common.Time;
global using Notrelix.Domain.SharedKernel;
global using Notrelix.Domain.Governance.Permissions;

// Workspace domain types
global using Notrelix.Domain.Workspaces.Members;
global using Notrelix.Domain.Workspaces.Spaces;
global using Notrelix.Domain.Workspaces.Teams;
global using Notrelix.Domain.Workspaces.Workspaces;

// WorkManagement domain types
global using Notrelix.Domain.WorkManagement.Boards;
global using Notrelix.Domain.WorkManagement.BoardGroups;
global using Notrelix.Domain.WorkManagement.Items;
global using Notrelix.Domain.WorkManagement.Fields;
global using Notrelix.Domain.WorkManagement.Views;
global using Notrelix.Domain.WorkManagement.Labels;
global using Notrelix.Domain.WorkManagement.Approvals;
