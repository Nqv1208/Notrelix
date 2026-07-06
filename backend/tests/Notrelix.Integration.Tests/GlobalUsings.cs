global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using Xunit;
global using FluentAssertions;
global using Moq;
global using Microsoft.EntityFrameworkCore;

// Application Common (capability folders)
global using Notrelix.Application.Common.Context;
global using Notrelix.Application.Common.Data.Rls;
global using Notrelix.Application.Common.Events;
global using Notrelix.Application.Common.Exceptions;
global using Notrelix.Application.Common.Integrations.N8n;
global using Notrelix.Application.Common.Messaging;
global using Notrelix.Application.Common.Security;
global using Notrelix.Application.Common.Security.Auth;
global using Notrelix.Application.Common.Tenancy;
global using Notrelix.Application.Common.Time;
