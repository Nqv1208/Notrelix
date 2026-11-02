# Notrelix — local development commands.
# Staging and production lifecycle is exclusively owned by the Delivery Platform.
.DEFAULT_GOAL := help
SHELL := /bin/bash
COMPOSE_STACK := docker compose -f docker-compose.yml
COMPOSE_DEV := docker compose -f docker-compose.dev.yml
ENV_DEV ?= --env-file .env.dev
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
	staging staging-up staging-down staging-logs prod prod-up prod-down prod-logs \
	build build-staging ps config-dev docs-generate docs-check ci-validate clean

help:
	@echo "Notrelix — local development"
	@echo "  make dev-up/dev-down/dev-logs"
	@echo "  make db-* / be-*"
	@echo "  make ci-validate"
	@echo ""
	@echo "Staging/production deploy is intentionally unavailable from Make."
	@echo "Use GitHub Actions: Release Candidate / Promote Staging-Verified Release."

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
	$(COMPOSE_DEV) $(ENV_DEV) exec postgres psql -U $${POSTGRES_USER:-postgres} -d $${POSTGRES_DB:-notrelix_dev}

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

# Delivery Platform owns all non-development environments. These targets intentionally
# fail rather than rebuilding or deploying bytes that bypass the release manifest.
staging staging-up prod prod-up build build-staging:
	@echo "ERROR: staging/production build/deploy is owned by the Notrelix Delivery Platform." >&2
	@echo "Use GitHub Actions Release Candidate / Promote Staging-Verified Release." >&2
	@exit 2

staging-down staging-logs prod-down prod-logs:
	@echo "ERROR: non-development environment lifecycle is an operator/CD action." >&2
	@exit 2

ps:
	docker compose -f docker-compose.yml ps -a 2>/dev/null || true
	$(COMPOSE_DEV) $(ENV_DEV) ps -a 2>/dev/null || true

config-dev:
	@JWT_SECRET=dev-only-not-for-production-but-at-least-32-chars!! $(COMPOSE_DEV) $(ENV_DEV) config

docs-generate:
	node scripts/docs/generate-document-index.mjs
	node scripts/docs/generate-rule-index.mjs
	node scripts/docs/generate-backend-project-map.mjs
	cd frontend && pnpm --filter @notrelix/dependency-rules docs:generate

docs-check:
	node scripts/docs/check-links.mjs
	node scripts/docs/check-metadata.mjs
	node scripts/docs/check-authority.mjs
	node scripts/docs/check-rule-ids.mjs
	node scripts/docs/check-source-inventory.mjs
	node scripts/docs/check-generated.mjs

ci-validate:
	python3 -m tools.deliveryctl validate
	python3 -m tools.deliveryctl architecture-check
	python3 -m unittest discover -s tools/deliveryctl/tests -p 'test_*.py' -v

clean:
	$(COMPOSE_DEV) $(ENV_DEV) down -v
