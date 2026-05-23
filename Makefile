# Notrelix — Docker Compose
# Dev: một file docker-compose.dev.yml (standalone, publish ports rõ ràng)
# Stack: docker-compose.yml = include infra + app (release)

COMPOSE_STACK := docker compose -f docker-compose.yml
COMPOSE_DEV := docker compose -f docker-compose.dev.yml
ENV_DEV ?= --env-file .env.dev
ENV_STAGING ?= --env-file .env.staging
ENV_PROD ?= --env-file .env.prod

.PHONY: help dev dev-up dev-down dev-logs dev-tools staging staging-up staging-down prod prod-up prod-down build ps clean config-dev

help:
	@echo "Notrelix — Docker"
	@echo ""
	@echo "Development (infra + dotnet watch + bun dev, gateway :3080):"
	@echo "  make dev-up       $(COMPOSE_DEV) $(ENV_DEV) up -d"
	@echo "  make dev-tools    + pgAdmin (profile tools)"
	@echo ""
	@echo "Staging / Prod (build images):"
	@echo "  make staging-up   cần .env.staging — xem config/docker/env.staging.example"
	@echo "  make prod-up      cần .env.prod"
	@echo ""
	@echo "  make config-dev   in ra compose đã merge (kiểm tra)"

dev: dev-up

dev-up:
	$(COMPOSE_DEV) $(ENV_DEV) up -d

dev-tools:
	$(COMPOSE_DEV) $(ENV_DEV) --profile tools up -d

dev-down:
	$(COMPOSE_DEV) $(ENV_DEV) down

dev-logs:
	$(COMPOSE_DEV) $(ENV_DEV) logs -f

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
	docker compose -f docker-compose.yml ps -a 2>/dev/null; $(COMPOSE_DEV) ps -a 2>/dev/null || true

config-dev:
	@JWT_SECRET=dev $(COMPOSE_DEV) $(ENV_DEV) config

clean:
	$(COMPOSE_DEV) $(ENV_DEV) down -v
	$(COMPOSE_STACK) $(ENV_STAGING) -f docker-compose.staging.yml down -v 2>/dev/null || true
	$(COMPOSE_STACK) $(ENV_PROD) -f docker-compose.prod.yml down -v 2>/dev/null || true
