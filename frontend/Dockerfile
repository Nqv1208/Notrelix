# Notrelix authenticated web container. Build/runtime bases are digest-pinned
# and updated centrally through delivery/images.lock.toml.
ARG NODE_BASE_IMAGE=node:22-alpine@sha256:c610fcdfb1d5b4740dd70c284ed3cb16bb857e0f7166196e36a5501df7a3aa32
ARG NGINX_BASE_IMAGE=nginx:alpine@sha256:db35bfc6b2951e7f8a72db5db120288c127ffaeeb4a6d4b95a26fead017d5913

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
RUN rm -f /etc/nginx/conf.d/default.conf \
    && mkdir -p /var/cache/nginx /var/run \
    && chown -R nginx:nginx /var/cache/nginx /var/run /usr/share/nginx/html
COPY --chown=nginx:nginx frontend/apps/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=builder --chown=nginx:nginx /app/apps/web/dist /usr/share/nginx/html
USER nginx
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://127.0.0.1:8080/ || exit 1
CMD ["nginx", "-g", "daemon off;"]
