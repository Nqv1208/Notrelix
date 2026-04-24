using Notrelix.Infrastructure.Data;
using Notrelix.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.AddInfrastructureServices();
builder.AddApplicationServices();
builder.AddWebServices();

// Swagger với JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Initialise and seed database
using var scope = app.Services.CreateScope();
var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

await initialiser.InitialiseAsync(); // ✅ luôn chạy

if (app.Environment.IsDevelopment())
{
    await initialiser.SeedAsync(); // chỉ dev
}

// Middleware pipeline
app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

// Trong Docker dev chỉ bind HTTP :8000 — redirect HTTPS gây lỗi khi mở Swagger qua http://...
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
