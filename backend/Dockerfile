# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Notrelix.Domain/Notrelix.Domain.csproj", "src/Notrelix.Domain/"]
COPY ["src/Notrelix.Application/Notrelix.Application.csproj", "src/Notrelix.Application/"]
COPY ["src/Notrelix.Infrastructure/Notrelix.Infrastructure.csproj", "src/Notrelix.Infrastructure/"]
COPY ["src/Notrelix.API/Notrelix.API.csproj", "src/Notrelix.API/"]

RUN dotnet restore "src/Notrelix.API/Notrelix.API.csproj"

# Copy source code and publish
COPY . .
WORKDIR "/src/src/Notrelix.API"

RUN dotnet publish "Notrelix.API.csproj" \
  -c Release \
  -o /app/publish \
  /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
  && apt-get install -y --no-install-recommends wget ca-certificates \
  && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8000

USER ubuntu

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8000/health/live || exit 1

ENTRYPOINT ["dotnet", "Notrelix.API.dll"]
