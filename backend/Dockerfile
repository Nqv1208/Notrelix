# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0.400 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS final
WORKDIR /app

RUN apt-get update \
  && apt-get install -y --no-install-recommends wget ca-certificates \
  && rm -rf /var/lib/apt/lists/*

RUN useradd -m -u 1000 appuser

COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

USER appuser

EXPOSE 8000

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8000/Health/live || exit 1

# Run the app
ENTRYPOINT ["dotnet", "Notrelix.API.dll"]
