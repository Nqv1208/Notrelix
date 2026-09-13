# Immutable build/runtime bases. CI supplies these arguments from
# delivery/images.lock.toml; defaults keep local builds on the same base bytes.
ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:9.0.317@sha256:f190d2dd9eef2899c91ac323caa0bd2b39334a5400ba93013e5199da39dad940
ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:9.0.20-bookworm-slim@sha256:779b0b298964fe93e6c6388c8bb823393a7c301b79cd5b3d80632bc886b2ffd4

FROM ${DOTNET_SDK_IMAGE} AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /workspace

COPY ["global.json", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/Notrelix.Domain/Notrelix.Domain.csproj", "src/Notrelix.Domain/"]
COPY ["src/Notrelix.Application/Notrelix.Application.csproj", "src/Notrelix.Application/"]
COPY ["src/Notrelix.Infrastructure/Notrelix.Infrastructure.csproj", "src/Notrelix.Infrastructure/"]
COPY ["src/Notrelix.API/Notrelix.API.csproj", "src/Notrelix.API/"]

# Do not hide a failed locked restore behind a second unlocked restore: it gives a false
# reproducibility guarantee. Notrelix currently pins direct versions centrally
# but does not commit NuGet lock files, so the build performs one authoritative
# restore mode until repo-level package locks are deliberately introduced.
RUN dotnet restore "src/Notrelix.API/Notrelix.API.csproj"

COPY . .
WORKDIR /workspace/src/Notrelix.API
RUN dotnet publish "Notrelix.API.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:ContinuousIntegrationBuild=true

FROM ${DOTNET_RUNTIME_IMAGE} AS final
WORKDIR /app
# The pinned runtime base can lag a Debian security archive between .NET patch
# releases; explicitly installing the patched package keeps the runtime image
# remediated without an ignore, a threshold change, or an unpinned base.
RUN apt-get update \
    && apt-get install -y --no-install-recommends wget ca-certificates libpcre2-8-0 \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --system notrelix \
    && useradd --system --gid notrelix --no-create-home --shell /usr/sbin/nologin notrelix
COPY --from=build --chown=notrelix:notrelix /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8000 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0
EXPOSE 8000
USER notrelix
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8000/health/live || exit 1
ENTRYPOINT ["dotnet", "Notrelix.API.dll"]
