# syntax=docker/dockerfile:1
#
# Security-patched postgres runtime image: the pinned official image with ONE binary replaced.
#
# Why: the official postgres:16-bookworm image ships /usr/local/bin/gosu built with Go 1.24.6, which
# Trivy flags with fixed-available Go stdlib findings. Upstream has not rebuilt gosu. This image keeps
# the official image byte-for-byte except /usr/local/bin/gosu, which is rebuilt from the same upstream
# release (tianon/gosu 1.19, commit pinned below) with a patched Go toolchain. Postgres major/minor,
# entrypoint, cmd, user, volumes and ports are inherited unchanged from the base.
#
# Build and publication: .github/workflows/postgres-image.yml builds this file, gates it with Trivy
# WITHOUT any ignore file, proves parity with the base image, and (trusted main only) publishes it to
# GHCR and attests it. The published digest is what delivery/images.lock.toml [images.postgres] pins.
#
# Rebuild obligation: every new official postgres digest requires bumping POSTGRES_BASE below, running
# the workflow, and re-pinning the lock to the new derived digest, until upstream ships a patched gosu.
# Reassessment condition: DROP this derived image (pin the official digest again) as soon as an official
# postgres digest ships gosu built with Go >= 1.26.10 / 1.27.3 and passes the runtime-image gate unaided.
#
# Local proof build:
#   docker build -f infra/postgres/Dockerfile -t notrelix-postgres:gosu-proof infra/postgres
ARG POSTGRES_BASE=postgres@sha256:0ea6700a3b4f0ae6ce746519073558aed4d88a79d8d07622a9a644946c7319c4
ARG GO_IMAGE=golang:1.26.10-bookworm@sha256:632dff7351799a5a9954a48dc06670ade72ee44e8c351cdc0e5a57af2fea21af

FROM ${GO_IMAGE} AS gosu-build
# tianon/gosu release 1.19 (the version the official image ships); the commit is pinned, the tag is not trusted.
ARG GOSU_COMMIT=6456aaa0f3c854d199d0f037f068eb97515b7513
ARG TARGETOS TARGETARCH
WORKDIR /src
RUN git init -q . \
 && git remote add origin https://github.com/tianon/gosu.git \
 && git fetch -q --depth 1 origin "${GOSU_COMMIT}" \
 && git checkout -q FETCH_HEAD
RUN CGO_ENABLED=0 GOOS="${TARGETOS:-linux}" GOARCH="${TARGETARCH:-$(go env GOARCH)}" \
    go build -trimpath -ldflags '-d -w' -o /out/gosu . \
 && /out/gosu --version

FROM ${POSTGRES_BASE}
LABEL org.opencontainers.image.source="https://github.com/Nqv1208/Notrelix" \
      org.opencontainers.image.description="postgres 16.15-bookworm with gosu rebuilt on a patched Go toolchain"
COPY --from=gosu-build --chmod=0755 /out/gosu /usr/local/bin/gosu
RUN gosu nobody true && gosu --version
