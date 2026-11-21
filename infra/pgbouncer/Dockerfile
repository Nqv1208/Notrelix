# syntax=docker/dockerfile:1
#
# Security-patched pgbouncer runtime image: the pinned upstream image plus an OS package upgrade only.
#
# Why: edoburu/pgbouncer:v1.25.2-p0 (alpine 3.23.4) ships libssl3/libcrypto3, libpq, musl, zlib and other
# packages with fixed-available HIGH findings, and upstream has not rebuilt the tag. This image changes
# nothing but `apk upgrade`; user, entrypoint, cmd and ports are inherited unchanged from the base.
#
# Build and publication: .github/workflows/pgbouncer-image.yml builds this file, gates it with Trivy
# WITHOUT any ignore file and publishes it to GHCR on trusted main. The published digest is what
# delivery/images.lock.toml [images.pgbouncer] pins.
#
# Rebuild obligation: every new upstream digest requires bumping PGBOUNCER_BASE and re-pinning the lock.
# Reassessment condition: DROP this derived image as soon as an upstream digest passes the gate unaided.
ARG PGBOUNCER_BASE=edoburu/pgbouncer@sha256:7d7a27d9e90985cab5cf42256f5c13a3120baa4b055b69df37beb272b89b2340
FROM ${PGBOUNCER_BASE}
USER root
RUN apk upgrade --no-cache
USER postgres
