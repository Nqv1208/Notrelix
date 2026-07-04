// Framework
global using MediatR;
global using FluentValidation;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Http.HttpResults;
global using Microsoft.OpenApi.Models;

// Domain
global using Notrelix.Domain.SharedKernel;
global using Notrelix.Domain.WorkManagement;
global using Notrelix.Domain.WorkManagement.Boards;
global using Notrelix.Domain.Workspaces.Members;
global using Notrelix.Domain.Documents.Blocks;

// Application Common (capability folders)
global using Notrelix.Application.Common.Context;
global using Notrelix.Application.Common.Exceptions;
global using Notrelix.Application.Common.Messaging;
global using Notrelix.Application.Common.RateLimiting;
global using Notrelix.Application.Common.Security.Auth;
