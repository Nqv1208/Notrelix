# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Notrelix.Domain/Notrelix.Domain.csproj", "Notrelix.Domain/"]
COPY ["Notrelix.Application/Notrelix.Application.csproj", "Notrelix.Application/"]
COPY ["Notrelix.Infrastructure/Notrelix.Infrastructure.csproj", "Notrelix.Infrastructure/"]
COPY ["Notrelix.API/Notrelix.API.csproj", "Notrelix.API/"]
RUN dotnet restore "Notrelix.API/Notrelix.API.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/Notrelix.API"
RUN dotnet build "Notrelix.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Notrelix.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

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
ENTRYPOINT ["dotnet", "Notrelix.API.dll"]
