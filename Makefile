# Notrelix — Docker Compose

.DEFAULT_GOAL := help

COMPOSE_STACK := docker compose -f docker-compose.yml
COMPOSE_DEV := docker compose -f docker-compose.dev.yml

ENV_DEV ?= --env-file .env.dev
ENV_STAGING ?= --env-file .env.staging
ENV_PROD ?= --env-file .env.prod

BACKEND_PROJECT := src/Notrelix.API/Notrelix.API.csproj

BACKEND_RUN := $(COMPOSE_DEV) $(ENV_DEV) run --rm --no-deps backend
DOTNET_RUN_API := dotnet run --project $(BACKEND_PROJECT) --no-launch-profile --no-restore

.PHONY: help \
	dev dev-up dev-down dev-restart dev-logs backend-logs dev-tools dev-clean dev-reset dev-reset-full \
	db-up db-restore db-restore-force db-migrate db-seed db-init db-rls db-psql \
	staging staging-up staging-down staging-logs \
	prod prod-up prod-down prod-logs \
	build build-staging ps config-dev clean

help:
	@echo "Notrelix — Docker"
	@echo ""
	@echo "Development:"
	@echo "  make dev-up          Start dev stack"
	@echo "  make dev-logs        Follow dev logs"
	@echo "  make backend-logs    Follow backend logs"
	@echo "  make dev-down        Stop dev stack"
	@echo "  make dev-restart     Restart dev stack"
	@echo "  make dev-clean       Stop dev stack and delete volumes"
	@echo "  make dev-reset       Delete dev volumes, migrate, seed, start"
	@echo "  make dev-reset-full  Delete dev volumes, force restore, migrate, seed, start"
	@echo "  make dev-tools       Start tools profile, including pgAdmin"
	@echo ""
	@echo "Database:"
	@echo "  make db-up           Start postgres/redis only"
	@echo "  make db-restore      Restore backend dependencies"
	@echo "  make db-restore-force Force restore backend dependencies"
	@echo "  make db-migrate      Run EF migrations"
	@echo "  make db-seed         Run seed data"
	@echo "  make db-init         Run migrations + seed"
	@echo "  make db-rls          Apply RLS policies"
	@echo "  make db-psql         Open psql"
	@echo ""
	@echo "Config:"
	@echo "  make config-dev      Print resolved dev compose config"

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

dev-clean:
	$(COMPOSE_DEV) $(ENV_DEV) down -v

dev-reset:
	$(COMPOSE_DEV) $(ENV_DEV) down -v
	$(COMPOSE_DEV) $(ENV_DEV) up -d postgres redis
	$(BACKEND_RUN) dotnet restore $(BACKEND_PROJECT)
	$(BACKEND_RUN) $(DOTNET_RUN_API) -- --migrate --seed
	$(COMPOSE_DEV) $(ENV_DEV) up -d

dev-reset-full:
	$(COMPOSE_DEV) $(ENV_DEV) down -v
	$(COMPOSE_DEV) $(ENV_DEV) up -d postgres redis
	$(BACKEND_RUN) dotnet restore $(BACKEND_PROJECT) --force
	$(BACKEND_RUN) $(DOTNET_RUN_API) -- --migrate --seed
	$(COMPOSE_DEV) $(ENV_DEV) up -d

be-restore: db-up
	$(BACKEND_RUN) dotnet restore $(BACKEND_PROJECT)

be-restore-force: db-up
	$(BACKEND_RUN) dotnet restore $(BACKEND_PROJECT) --force

db-up:
	$(COMPOSE_DEV) $(ENV_DEV) up -d postgres redis

db-migrate: db-up
	$(BACKEND_RUN) $(DOTNET_RUN_API) -- --migrate

db-seed: db-up
	$(BACKEND_RUN) $(DOTNET_RUN_API) -- --seed

db-init: db-up
	$(BACKEND_RUN) $(DOTNET_RUN_API) -- --migrate --seed

db-rls: db-up
	$(BACKEND_RUN) $(DOTNET_RUN_API) -- --rls-apply

db-psql:
	$(COMPOSE_DEV) $(ENV_DEV) exec postgres \
		psql -U $${POSTGRES_USER:-postgres} -d $${POSTGRES_DB:-notrelix_dev}

staging: staging-up

staging-up:
	$(COMPOSE_STACK) $(ENV_STAGING) -f docker-compose.staging.yml up -d --build

staging-down:
	$(COMPOSE_STACK) $(ENV_STAGING) -f docker-compose.staging.yml down

staging-logs:
	$(COMPOSE_STACK) $(ENV_STAGING) -f docker-compose.staging.yml logs -f

prod: prod-up

prod-up:
	$(COMPOSE_STACK) $(ENV_PROD) -f docker-compose.prod.yml up -d --build

prod-down:
	$(COMPOSE_STACK) $(ENV_PROD) -f docker-compose.prod.yml down

prod-logs:
	$(COMPOSE_STACK) $(ENV_PROD) -f docker-compose.prod.yml logs -f

build:
	$(COMPOSE_STACK) $(ENV_PROD) -f docker-compose.prod.yml build

build-staging:
	$(COMPOSE_STACK) $(ENV_STAGING) -f docker-compose.staging.yml build

ps:
	docker compose -f docker-compose.yml ps -a 2>/dev/null || true
	$(COMPOSE_DEV) $(ENV_DEV) ps -a 2>/dev/null || true

config-dev:
	@JWT_SECRET=dev-only-not-for-production-but-at-least-32-chars!! $(COMPOSE_DEV) $(ENV_DEV) config

clean:
	$(COMPOSE_DEV) $(ENV_DEV) down -v
	$(COMPOSE_STACK) $(ENV_STAGING) -f docker-compose.staging.yml down -v 2>/dev/null || true
	$(COMPOSE_STACK) $(ENV_PROD) -f docker-compose.prod.yml down -v 2>/dev/null || true