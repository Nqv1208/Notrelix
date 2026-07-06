FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /workspace

COPY ["global.json", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

COPY ["src/Notrelix.Domain/Notrelix.Domain.csproj", "src/Notrelix.Domain/"]
COPY ["src/Notrelix.Application/Notrelix.Application.csproj", "src/Notrelix.Application/"]
COPY ["src/Notrelix.Infrastructure/Notrelix.Infrastructure.csproj", "src/Notrelix.Infrastructure/"]
COPY ["src/Notrelix.API/Notrelix.API.csproj", "src/Notrelix.API/"]

RUN dotnet restore "src/Notrelix.API/Notrelix.API.csproj"

COPY . .

WORKDIR /workspace/src/Notrelix.API

RUN dotnet publish "Notrelix.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends wget ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --system notrelix \
    && useradd --system --gid notrelix --create-home notrelix

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8000
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8000

USER notrelix

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8000/health/live || exit 1

ENTRYPOINT ["dotnet", "Notrelix.API.dll"]
