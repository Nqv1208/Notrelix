using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Middleware;
using Notrelix.API.Endpoints;
using Notrelix.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationServices();
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApiLayer(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();
    await initialiser.InitialiseAsync();

    if (app.Configuration.GetValue<bool>("SeedData:Enabled"))
    {
        await initialiser.SeedAsync();
    }
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

if (app.Configuration.GetValue<bool>("HttpsRedirection:Enabled"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseWorkspaceResolution();
app.UseAuthorization();

app.MapEndpoints();

app.Run();
