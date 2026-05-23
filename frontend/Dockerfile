# syntax=docker/dockerfile:1.7
# Production image: Bun cài dependency; Node chạy `next build` (Next.js 16 + Turbopack cần N-API Node).
# Build args: NEXT_PUBLIC_* được embed lúc build.

FROM oven/bun:1.2-alpine AS deps
WORKDIR /app
RUN apk update && apk upgrade --no-cache
COPY package.json bun.lock ./
RUN bun install --frozen-lockfile

FROM node:22-alpine AS builder
WORKDIR /app
RUN apk update && apk upgrade --no-cache
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ARG NEXT_PUBLIC_API_URL=/api/v1
ARG NEXT_PUBLIC_APP_ENV=production
ENV NEXT_PUBLIC_API_URL=${NEXT_PUBLIC_API_URL}
ENV NEXT_PUBLIC_APP_ENV=${NEXT_PUBLIC_APP_ENV}
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN node ./node_modules/next/dist/bin/next build

FROM node:22-alpine AS runner
WORKDIR /app
RUN apk update && apk upgrade --no-cache \
  && addgroup -S nextjs && adduser -S nextjs -G nextjs
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
COPY --from=builder --chown=nextjs:nextjs /app/public ./public
COPY --from=builder --chown=nextjs:nextjs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nextjs /app/.next/static ./.next/static
USER nextjs
EXPOSE 3000
CMD ["node", "server.js"]
