# Notrelix authenticated web container. Build/runtime bases are digest-pinned
# and updated centrally through delivery/images.lock.toml.
ARG NODE_BASE_IMAGE=node:22-alpine@sha256:c610fcdfb1d5b4740dd70c284ed3cb16bb857e0f7166196e36a5501df7a3aa32
ARG NGINX_BASE_IMAGE=nginx:alpine@sha256:1ed1b0e1d7652937d6cbdaf4018c7b6fc009a7dd6c3047351e2eddda745de43f

FROM ${NODE_BASE_IMAGE} AS base
RUN corepack enable && corepack prepare pnpm@10.0.0 --activate
WORKDIR /repo

FROM base AS pruner
COPY frontend ./frontend
WORKDIR /repo/frontend
# Exact CLI version keeps prune semantics stable. The generated pruned lockfile
# remains the authority for the subsequent install.
RUN pnpm dlx turbo@2.10.5 prune @notrelix/app-web --docker

FROM base AS builder
WORKDIR /app
COPY --from=pruner /repo/frontend/out/json/ ./
COPY --from=pruner /repo/frontend/out/pnpm-lock.yaml ./pnpm-lock.yaml
COPY frontend/.npmrc ./.npmrc
RUN pnpm install --frozen-lockfile
COPY --from=pruner /repo/frontend/out/full/ ./
RUN pnpm --filter @notrelix/app-web build

FROM ${NGINX_BASE_IMAGE} AS production
# CVE-2026-93990 (HIGH): the locked nginx:alpine digest (alpine 3.24.2) still
# bakes libexpat 2.8.4-r0 while alpine v3.24 publishes the fixed 2.8.5-r0 and no
# newer upstream digest is available yet. The floor constraint rolls forward with
# the repository instead of hard-breaking the build on the next point release;
# remove this layer once the base-image lock advances past libexpat 2.8.5-r0.
RUN apk add --no-cache "libexpat>=2.8.5"
# Full nginx configuration authority: pid and all writable temp paths live in
# tmpfs-backed /tmp, so the image can run entirely as the unprivileged nginx
# user without relying on upstream internal directory ownership.
COPY frontend/apps/web/nginx.conf /etc/nginx/nginx.conf
COPY --from=builder --chown=nginx:nginx /app/apps/web/dist /usr/share/nginx/html
USER nginx
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://127.0.0.1:8080/ || exit 1
CMD ["nginx", "-g", "daemon off;"]
