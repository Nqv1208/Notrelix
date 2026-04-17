# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["TodoApp.Domain/TodoApp.Domain.csproj", "TodoApp.Domain/"]
COPY ["TodoApp.Application/TodoApp.Application.csproj", "TodoApp.Application/"]
COPY ["TodoApp.Infrastructure/TodoApp.Infrastructure.csproj", "TodoApp.Infrastructure/"]
COPY ["TodoApp.Web/TodoApp.Web.csproj", "TodoApp.Web/"]
RUN dotnet restore "TodoApp.Web/TodoApp.Web.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/TodoApp.Web"
RUN dotnet build "TodoApp.Web.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TodoApp.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create non-root user for security
RUN useradd -m appuser

# Copy published app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8000
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8000/health || exit 1

# Run the app
ENTRYPOINT ["dotnet", "TodoApp.Web.dll"]
