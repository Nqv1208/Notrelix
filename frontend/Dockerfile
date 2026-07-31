# Multi-stage Dockerfile for Notrelix Frontend

# Stage 1: Install dependencies
FROM node:22-alpine AS base
RUN corepack enable
WORKDIR /app

# Stage 2: Build web app
FROM base AS builder
RUN corepack enable pnpm@10.0.0

COPY frontend/package.json frontend/pnpm-lock.yaml frontend/pnpm-workspace.yaml ./
COPY frontend/turbo.json ./
COPY frontend/tooling ./tooling
COPY frontend/packages ./packages
COPY frontend/apps/web ./apps/web

RUN pnpm install --frozen-lockfile
RUN pnpm --filter @notrelix/app-web build

# Stage 3: Production image
FROM nginx:alpine AS production

# Copy nginx config
COPY frontend/apps/web/nginx.conf /etc/nginx/conf.d/default.conf

# Copy built assets
COPY --from=builder /app/apps/web/dist /usr/share/nginx/html

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
