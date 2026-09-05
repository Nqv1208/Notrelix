// Framework
global using System.IdentityModel.Tokens.Jwt;
global using System.Security.Claims;
global using System.Security.Cryptography;
global using System.Text;
global using MassTransit;
global using MediatR;
global using Microsoft.AspNetCore.Authentication.JwtBearer;
global using Microsoft.AspNetCore.DataProtection;
global using Microsoft.AspNetCore.Http;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Diagnostics;
global using Microsoft.EntityFrameworkCore.Design;
global using Microsoft.EntityFrameworkCore.Infrastructure;
global using Microsoft.EntityFrameworkCore.Migrations;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
global using Microsoft.Extensions.Caching.Distributed;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.IdentityModel.Tokens;
global using Npgsql;
global using NpgsqlTypes;
global using Resend;
global using StackExchange.Redis;

// Domain
global using Notrelix.Domain.Common;
global using Notrelix.Domain.SharedKernel;

// Application Common (capability folders)
global using Notrelix.Application.Common.Auditing;
global using Notrelix.Application.Common.Caching;
global using Notrelix.Application.Common.Context;
global using Notrelix.Application.Common.Data;
global using Notrelix.Application.Common.Data.Rls;
global using Notrelix.Application.Common.Email;
global using Notrelix.Application.Common.Events;
global using Notrelix.Application.Common.Idempotency;
global using Notrelix.Application.Features.Integrations.N8n.Providers;
global using Notrelix.Application.Features.Integrations.N8n.Services;
global using Notrelix.Application.Features.Automation.CrossContext.WorkManagement;
global using Notrelix.Application.Common.Messaging;
global using Notrelix.Application.Common.Realtime;
global using Notrelix.Application.Common.RateLimiting;
global using Notrelix.Application.Common.Security;
global using Notrelix.Application.Common.Security.Auth;
global using Notrelix.Application.Common.Storage;
global using Notrelix.Application.Common.Tenancy;
global using Notrelix.Application.Common.Time;
