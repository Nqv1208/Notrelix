# Notrelix Delivery Control Plane (V4 Hardened)

This directory is the machine-readable authority for CI/CD routing, proof obligations, release composition, deployment policy and immutable image inputs.

- `catalog.toml` — component IDs, providers, roots/workspaces, build/container/deploy contracts and the component proof profile.
- `policy.toml` — change routing, proof profiles/bindings, release intent and migration/rollback policy.
- `environments.toml` — environment adapter policy actually consumed by deployment (`compose_overlay`, promotion mode, migration execution, smoke profile and stateful-image policy).
- `images.lock.toml` — the only build/runtime image lock; every subject is immutable.

## Authority rules

1. Workflows MUST consume this model through `scripts/ci/*`; they MUST NOT recreate product routing with independent `paths`, component lists or proof IDs.
2. `build-plan.py` selects proof **profiles**. Concrete proof IDs live only in `policy.toml`.
3. `images.lock.toml` is the only runtime/build image-lock authority. Legacy env lock files or committed release overlays are forbidden.
4. Environment names equal GitHub Environment names. Job-level GitHub Environment/concurrency are generic functions of `environment_name`; fields that cannot be enforced from this file are intentionally not modeled here.
5. Schema-changing releases are identified by `deployment.migration_paths` and carry that fact in the sealed release manifest. Once a schema-changing migration starts, automatic application rollback is disabled.

## Extension rule

Adding another component of an existing provider should normally require component code + one catalog entry. The planner, evidence aggregator and release engine stay unchanged.

Adding another **manual-promotion** environment using the same Compose adapter requires one `environments.toml` entry plus the matching protected GitHub Environment. The generic `promote-release.yml` workflow accepts it without a new environment-specific workflow.

A genuinely new provider/runtime/proof model may add a provider lane and proof profile, but must not change evidence or promotion semantics.

## Container topology authority

The delivery model is consumed together with the repository runtime topology. V4 owns `docker-compose.yml`, both deployment overlays, the backend/web/marketing Dockerfiles, `infra/nginx/nginx.conf`, `infra/nginx/nginx.prod.conf`, and `frontend/apps/web/nginx.conf`. Keep RabbitMQ mandatory in Staging/Production and keep production origin TLS edge-terminated unless the architecture is explicitly superseded.
