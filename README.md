# Notrelix

**One workspace for structured work, documents and conversation, built for teams that need governance.**

Notrelix is an enterprise work-management platform for teams that outgrow a pile of separate task, wiki and chat tools. Boards, documents and chat share one workspace, one identity and one permission model. Each business fact has a single owner, and Work Management follows one rule: one work model, many views.

Live demo (staging): <https://demo.notrelix.me>

## What you can do

- **Plan work.** Manage one work model through Board, Table, Calendar and Timeline views.
- **Write together.** Create documents and collaborate on them in the workspace, next to the work they describe.
- **Talk.** Use channels, threads and direct messages, plus voice and video calls.
- **Organise people.** Run workspaces and teams, with invitations, roles and membership.
- **Sign in securely.** Use enterprise identity: SSO (SAML and OIDC), multi-factor authentication and passkeys.
- **Stay in control.** Apply governance and permissions across every module, enforced in the application and again in PostgreSQL with row-level security.
- **Coming:** Billing and Analytics are specified but not yet available.

Per-module delivery state is tracked in [the use-case catalog](docs/workstreams/use-cases/README.md).

## Quick start

Requires Git, Docker with Compose, and (for local code work) Node.js 22+, pnpm 10+ and the .NET 9 SDK.

```bash
git clone https://github.com/Nqv1208/Notrelix.git
cd Notrelix
cp .env.example .env.dev     # fill in database, Redis and JWT values
make dev-up                  # start the development stack
make dev-logs                # follow logs
make dev-down                # stop
```

| Service                | URL                   |
| ---------------------- | --------------------- |
| Gateway (use this one) | http://localhost:3080 |
| Web app                | http://localhost:5173 |
| Marketing site         | http://localhost:3000 |
| Backend API            | http://localhost:8000 |

Optional: `make messaging-up` for RabbitMQ, `make dev-tools` for pgAdmin. `make help` lists the rest.

## Tech stack

| Layer                 | Technology                                                                  |
| --------------------- | --------------------------------------------------------------------------- |
| Backend               | .NET 9, ASP.NET Core, modular monolith, DDD, CQRS-style use cases (MediatR) |
| Data                  | PostgreSQL 16 with row-level security, EF Core                              |
| Runtime services      | Redis 7, RabbitMQ with MassTransit                                          |
| Web                   | React 19, Vite, TanStack Router and Query, Tailwind CSS 4                   |
| Marketing             | Next.js                                                                     |
| Mobile                | Expo, React Native                                                          |
| Monorepo and delivery | pnpm and Turborepo, Docker Compose, Nginx, GitHub Actions                   |

Exact versions live in the manifests and lockfiles, not here.

## Learn more

- [Product constitution](PRODUCT.md) and [design constitution](DESIGN.md)
- [Domain language](CONTEXT.md) and [context map](CONTEXT-MAP.md)
- [Repository and architecture overview](docs/architecture/overview.md): layers, packages, state model, contracts, Makefile targets, test topology
- [System architecture](docs/architecture/system-overview.md)
- [Delivery and CI/CD](docs/delivery/ci-cd-architecture.md) and [local development](docs/delivery/local-development.md)
- [Infrastructure and the Oracle VM runbook](docs/infrastructure/oracle-vm-runbook.md)
- [Use-case catalog](docs/workstreams/use-cases/README.md)
- [Contributing, rules and agent guide](AGENTS.md)
