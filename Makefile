# Notrelix — Docker Compose

.DEFAULT_GOAL := help

SHELL := /bin/bash

COMPOSE_STACK := docker compose -f docker-compose.yml
COMPOSE_DEV := docker compose -f docker-compose.dev.yml

ENV_DEV ?= --env-file .env.dev
ENV_STAGING ?= --env-file .env.staging
ENV_PROD ?= --env-file .env.prod

BACKEND_PROJECT := src/Notrelix.API/Notrelix.API.csproj
BACKEND_SOLUTION := backend.slnx

BACKEND_RUN := $(COMPOSE_DEV) $(ENV_DEV) run --rm --no-deps backend
BACKEND_EXEC := $(COMPOSE_DEV) $(ENV_DEV) exec backend

DOTNET_RESTORE_API := dotnet restore $(BACKEND_PROJECT)
DOTNET_RESTORE_API_FORCE := dotnet restore $(BACKEND_PROJECT) --force --no-cache
DOTNET_RUN_API := dotnet run --project $(BACKEND_PROJECT) --no-launch-profile --no-restore

.PHONY: help \
	dev dev-up dev-down dev-restart dev-logs backend-logs dev-tools dev-clean dev-reset dev-reset-full \
	messaging-up messaging-down \
	db-up db-restore db-restore-force be-restore be-restore-force db-migrate db-seed db-init db-rls db-psql \
	be-build be-test be-clean-nuget be-shell backend-image-build \
	staging staging-up staging-down staging-logs \
	prod prod-up prod-down prod-logs \
	build build-staging ps config-dev docs-check clean

help:
	@echo "Notrelix — Docker"
	@echo ""
	@echo "Development:"
	@echo "  make dev-up              Start dev stack"
	@echo "  make dev-logs            Follow dev logs"
	@echo "  make backend-logs        Follow backend logs"
	@echo "  make dev-down            Stop dev stack"
	@echo "  make dev-restart         Restart dev stack"
	@echo "  make dev-clean           Stop dev stack and delete volumes"
	@echo "  make dev-reset           Delete dev volumes, restore, migrate, seed, start"
	@echo "  make dev-reset-full      Delete dev volumes, force restore, migrate, seed, start"
	@echo "  make dev-tools           Start tools profile, including pgAdmin"
	@echo "  make messaging-up        Start RabbitMQ (messaging profile)"
	@echo "  make messaging-down      Stop RabbitMQ"
	@echo ""
	@echo "Database:"
	@echo "  make db-up               Start postgres/redis only"
	@echo "  make db-restore          Restore backend dependencies inside container"
	@echo "  make db-restore-force    Force restore backend dependencies inside container"
	@echo "  make db-migrate          Run EF migrations"
	@echo "  make db-seed             Run seed data"
	@echo "  make db-init             Run migrations + seed"
	@echo "  make db-rls              Apply RLS policies"
	@echo "  make db-psql             Open psql"
	@echo ""
	@echo "Backend:"
	@echo "  make be-build            Build backend inside container"
	@echo "  make be-test             Run backend tests inside container"
	@echo "  make be-clean-nuget      Clear NuGet caches inside container"
	@echo "  make be-shell            Open shell inside backend container"
	@echo "  make backend-image-build Rebuild backend Docker image"
	@echo ""
	@echo "Config:"
	@echo "  make config-dev          Print resolved dev compose config"
	@echo "  make docs-check          Validate documentation authority"

# ---------------------------------------------------------------------
# Development stack
# ---------------------------------------------------------------------

dev: dev-up

dev-up:
	$(COMPOSE_DEV) $(ENV_DEV) up -d

dev-down:
	$(COMPOSE_DEV) $(ENV_DEV) down

dev-restart: dev-down dev-up

dev-logs:
	$(COMPOSE_DEV) $(ENV_DEV) logs -f

backend-logs:
	$(COMPOSE_DEV) $(ENV_DEV) logs -f backend

dev-tools:
	$(COMPOSE_DEV) $(ENV_DEV) --profile tools up -d

messaging-up:
	$(COMPOSE_DEV) $(ENV_DEV) --profile messaging up -d rabbitmq

messaging-down:
	$(COMPOSE_DEV) $(ENV_DEV) --profile messaging down

dev-clean:
	$(COMPOSE_DEV) $(ENV_DEV) down -v

dev-reset:
	$(COMPOSE_DEV) $(ENV_DEV) down -v
	$(MAKE) db-up
	$(MAKE) db-restore
	$(BACKEND_RUN) $(DOTNET_RUN_API) -- --migrate --seed
	$(MAKE) db-rls
	$(COMPOSE_DEV) $(ENV_DEV) up -d

dev-reset-full:
	$(COMPOSE_DEV) $(ENV_DEV) down -v
	$(MAKE) db-up
	$(MAKE) be-clean-nuget
	$(MAKE) db-restore-force
	$(BACKEND_RUN) $(DOTNET_RUN_API) -- --migrate --seed
	$(MAKE) db-rls
	$(COMPOSE_DEV) $(ENV_DEV) up -d

# ---------------------------------------------------------------------
# Database commands
# ---------------------------------------------------------------------

db-up:
	$(COMPOSE_DEV) $(ENV_DEV) up -d postgres redis

db-restore: db-up
	$(BACKEND_RUN) $(DOTNET_RESTORE_API)

db-restore-force: db-up
	$(BACKEND_RUN) $(DOTNET_RESTORE_API_FORCE)

be-restore: db-restore

be-restore-force: db-restore-force

db-migrate: db-restore
	$(BACKEND_RUN) $(DOTNET_RUN_API) -- --migrate

db-seed: db-restore
	$(BACKEND_RUN) $(DOTNET_RUN_API) -- --seed

db-init: db-restore
	$(BACKEND_RUN) $(DOTNET_RUN_API) -- --migrate --seed

db-rls: db-restore
	$(BACKEND_RUN) $(DOTNET_RUN_API) -- --rls-apply

db-psql:
	$(COMPOSE_DEV) $(ENV_DEV) exec postgres \
		psql -U $${POSTGRES_USER:-postgres} -d $${POSTGRES_DB:-notrelix_dev}

# ---------------------------------------------------------------------
# Backend utilities
# ---------------------------------------------------------------------

be-build: db-restore
	$(BACKEND_RUN) dotnet build $(BACKEND_PROJECT) --no-restore

be-test: db-restore
	$(BACKEND_RUN) dotnet test --no-restore

be-clean-nuget:
	$(BACKEND_RUN) dotnet nuget locals all --clear

be-shell:
	$(COMPOSE_DEV) $(ENV_DEV) run --rm --no-deps backend bash

backend-image-build:
	$(COMPOSE_DEV) $(ENV_DEV) build backend

# ---------------------------------------------------------------------
# Staging
# ---------------------------------------------------------------------

staging: staging-up

staging-up:
	$(COMPOSE_STACK) $(ENV_STAGING) -f docker-compose.staging.yml up -d --build

staging-down:
	$(COMPOSE_STACK) $(ENV_STAGING) -f docker-compose.staging.yml down

staging-logs:
	$(COMPOSE_STACK) $(ENV_STAGING) -f docker-compose.staging.yml logs -f

# ---------------------------------------------------------------------
# Production
# ---------------------------------------------------------------------

prod: prod-up

prod-up:
	$(COMPOSE_STACK) $(ENV_PROD) -f docker-compose.prod.yml up -d --build

prod-down:
	$(COMPOSE_STACK) $(ENV_PROD) -f docker-compose.prod.yml down

prod-logs:
	$(COMPOSE_STACK) $(ENV_PROD) -f docker-compose.prod.yml logs -f

# ---------------------------------------------------------------------
# Build / config / cleanup
# ---------------------------------------------------------------------

build:
	$(COMPOSE_STACK) $(ENV_PROD) -f docker-compose.prod.yml build

build-staging:
	$(COMPOSE_STACK) $(ENV_STAGING) -f docker-compose.staging.yml build

ps:
	docker compose -f docker-compose.yml ps -a 2>/dev/null || true
	$(COMPOSE_DEV) $(ENV_DEV) ps -a 2>/dev/null || true

config-dev:
	@JWT_SECRET=dev-only-not-for-production-but-at-least-32-chars!! $(COMPOSE_DEV) $(ENV_DEV) config

docs-check:
	node scripts/check-documentation.mjs

clean:
	$(COMPOSE_DEV) $(ENV_DEV) down -v
	$(COMPOSE_STACK) $(ENV_STAGING) -f docker-compose.staging.yml down -v 2>/dev/null || true
	$(COMPOSE_STACK) $(ENV_PROD) -f docker-compose.prod.yml down -v 2>/dev/null || true
