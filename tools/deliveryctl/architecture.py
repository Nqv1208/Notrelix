from __future__ import annotations

import re
from pathlib import Path

from .model import load_authorities
from .planner import FRONTEND_FULL_BASELINE
from .runtime import ROOT

SHA40 = re.compile(r"^[0-9a-f]{40}$")
USES = re.compile(r"^\s*uses:\s*([^\s#]+)", re.MULTILINE)
PROVIDERS = {
    "backend-ci.yml",
    "frontend-ci.yml",
    "container-ci.yml",
    "infra-ci.yml",
    "docs-ci.yml",
    "security-ci.yml",
    "stack-smoke.yml",
}
LEGACY_EXECUTABLE_REFERENCES = (
    "scripts/ci/build-plan.py",
    "scripts/ci/delivery_model.py",
    "scripts/ci/component-info.py",
    "scripts/ci/image-info.py",
    "scripts/ci/environment-info.py",
    "scripts/ci/aggregate-evidence.py",
    "scripts/ci/prepare-deployment-bundle.py",
    "scripts/ci/render-images-env.py",
    "scripts/ci/render-release-compose.py",
    "setup-ci-python",
    "actions/setup-python@",
    ".python-version",
)


def check(root: Path = ROOT) -> None:
    authorities = load_authorities(root)
    errors: list[str] = []
    workflows = root / ".github/workflows"

    if (root / ".python-version").exists():
        errors.append(".python-version forbidden")
    if (root / ".github/actions/setup-ci-python").exists():
        errors.append("setup-ci-python forbidden")

    runtime_refs = {
        image_id: cfg["ref"]
        for image_id, cfg in authorities["images"]["images"].items()
        if cfg.get("class") == "runtime"
    }

    for path in sorted(workflows.glob("*.yml")):
        text = path.read_text(encoding="utf-8")
        if "permissions:" not in text:
            errors.append(f"{path.name}: explicit permissions missing")
        if re.search(r"runs-on:[^\n]*latest", text, re.IGNORECASE):
            errors.append(f"{path.name}: latest runner forbidden")

        for raw in USES.findall(text):
            ref = raw.strip("\"'")
            if ref.startswith("./") or ref.startswith("docker://"):
                continue
            if "@" not in ref or not SHA40.fullmatch(ref.rsplit("@", 1)[1]):
                errors.append(f"{path.name}: action not full-SHA pinned: {ref}")

        for legacy in LEGACY_EXECUTABLE_REFERENCES:
            if legacy in text:
                errors.append(f"{path.name}: legacy runtime reference {legacy}")

        if path.name in PROVIDERS:
            if "tools.deliveryctl" in text or "delivery/" in text:
                errors.append(f"{path.name}: provider reads control plane/authority")
            for image_id, ref in runtime_refs.items():
                if ref in text:
                    # W4/W5 transition exception: standalone provider lanes (until
                    # the W7 cutover) have no plan to receive runtime refs from,
                    # so each provider may single-source locked digests once in
                    # a FALLBACK_* env contract. Remove together with the
                    # standalone lanes; any second occurrence or use outside a
                    # transitional fallback stays forbidden.
                    transitional_fallback = (
                        path.name in {"backend-ci.yml", "container-ci.yml", "infra-ci.yml"}
                        and text.count(ref) == 1
                        and re.search(r"FALLBACK_[A-Z_]+:", text) is not None
                    )
                    if not transitional_fallback:
                        errors.append(f"{path.name}: duplicated runtime image authority {image_id}")

    for forbidden_ci in ("ci-v2.yml", "ci-orchestrator.yml", "ci-gate.yml"):
        if (workflows / forbidden_ci).exists():
            errors.append(f"central CI orchestrator {forbidden_ci} forbidden")

    ci_path = workflows / "ci.yml"
    if not ci_path.exists():
        errors.append("central control workflow ci.yml missing")
    else:
        ci_text = ci_path.read_text(encoding="utf-8")
        if not re.search(r"^name:\s*Notrelix CI\s*$", ci_text, re.MULTILINE):
            errors.append("ci.yml must be named Notrelix CI")
        if "python3 -m tools.deliveryctl plan" not in ci_text:
            errors.append("ci.yml must invoke canonical deliveryctl plan")
        for forbidden in (
            "dotnet test",
            "pnpm test",
            "pnpm e2e",
            "docker build",
            "docker compose build",
            "make docs-check",
        ):
            if forbidden in ci_text:
                errors.append(f"ci.yml must not implement domain checks ({forbidden})")

    developer_workflows = (
        "backend-ci.yml",
        "frontend-ci.yml",
        "docs-ci.yml",
        "infra-ci.yml",
        "container-ci.yml",
    )
    domain_workflows = developer_workflows + ("ci-definition.yml",)
    for name in developer_workflows:
        if "python3 -m tools.deliveryctl plan" in (workflows / name).read_text(encoding="utf-8"):
            errors.append(f"{name}: canonical deliveryctl planner forbidden in domain workflow")
    reusable_domain = domain_workflows + ("security-ci.yml",)
    for name in reusable_domain:
        text = (workflows / name).read_text(encoding="utf-8")
        if "uses: ./.github/actions/emit-evidence" in text and "workflow_call:" not in text:
            errors.append(f"{name}: domain evidence requires reusable workflow_call entry")
        if "workflow_call:" in text and not re.search(r"inputs\.", text):
            errors.append(f"{name}: workflow_call entry must consume resolved inputs")

    frontend = (workflows / "frontend-ci.yml").read_text(encoding="utf-8")
    if re.search(r"has [^\n]+&&\s*require [^\n]+\|\|\s*true", frontend):
        errors.append("frontend gate contains fail-open require/|| true pattern")
    if "check:production-mock-isolation" not in frontend:
        errors.append("frontend must verify production/mock artifact isolation")
    if "restore-host-artifact.mjs" not in frontend:
        errors.append("frontend exact-artifact restore contract missing")
    if re.search(r"pnpm\s+audit", frontend):
        errors.append("frontend provider must not duplicate dependency audit; security-ci.yml is the single owner")

    # Frontend CI process reorganization v1.1 guards (SC-FE-001..014).
    if re.search(r"^\s*(persona|state):\s*\[", frontend, re.MULTILINE):
        errors.append("frontend workflow must not expose persona/state combinations as a GitHub matrix (SC-FE-010)")
    if re.search(r"^\s*shard_index:\s*\[[^\]]+\]", frontend, re.MULTILINE):
        errors.append("frontend mock shards must expand from four include cells, not an array cross product (SC-FE-004)")
    shard_cells = re.findall(r"- \{shard_index: (\d+), shard_label: \"\d/4\"\}", frontend)
    if sorted(shard_cells) != ["0", "1", "2", "3"]:
        errors.append(f"frontend mock shard expansion must be exactly 4 cells (0..3), got {sorted(shard_cells)} (SC-FE-004)")
    if "e2e:real" in frontend:
        errors.append("frontend workflow must not introduce mandatory real-backend E2E (SC-FE-012)")
    host_runtime_start = frontend.find("\n  host-runtime:\n")
    if host_runtime_start < 0:
        errors.append("frontend workflow must define a host-runtime semantic stage (SC-FE-007)")
    else:
        host_tail = frontend[host_runtime_start + 1:]
        next_job = re.search(r"\n  [a-z][a-z0-9-]*:\n", host_tail)
        host_block = host_tail[: next_job.start()] if next_job else host_tail
        if re.search(r"(pnpm --filter [^\n]* build|pnpm build|npm run build|docker build)", host_block):
            errors.append("host runtime must consume the exact artifact and never rebuild (SC-FE-007)")
    gate_match = re.search(r"^  frontend-gate:\n((?:  .*\n|\n)*)", frontend, re.MULTILINE)
    if not gate_match:
        errors.append("frontend workflow must define the frontend-gate final closure job (SC-FE-009)")
    else:
        gate_block = gate_match.group(1)
        for required_stage in (
            "repository-integrity",
            "workspace-tests",
            "application-build",
            "host-runtime",
            "ui-system",
            "mock-system",
        ):
            if required_stage not in gate_block:
                errors.append(f"frontend gate must require the {required_stage} semantic stage (SC-FE-009)")
    profile_sources = re.findall(r'FALLBACK_PROFILE_JSON: \'([^\']+)\'', frontend)
    if not profile_sources:
        errors.append("frontend workflow must carry the frontend-full-baseline fallback profile (SC-FE-002)")
    else:
        try:
            import json as _json

            fallback_profile = _json.loads(profile_sources[0])
            if fallback_profile != FRONTEND_FULL_BASELINE:
                errors.append("frontend fallback profile must be the full migration profile (SC-FE-002)")
        except ValueError:
            errors.append("frontend fallback profile JSON is malformed (SC-FE-002)")

    # v1.1 closure guards (SC-FE-015..017): the profile is an executable contract —
    # no dynamic matrix relay, bounded full-baseline topology, explicit step-output contracts.
    if "application_build_matrix_json" in frontend or "host_runtime_matrix_json" in frontend:
        errors.append("frontend workflow must not relay dynamic build/runtime matrices through select outputs (SC-FE-015)")
    app_build_start = frontend.find("\n  application-build:\n")
    if app_build_start < 0:
        errors.append("frontend workflow must define the application-build semantic stage (SC-FE-016)")
    else:
        app_tail = frontend[app_build_start + 1:]
        next_app_job = re.search(r"\n  [a-z][a-z0-9-]*:\n", app_tail)
        app_block = app_tail[: next_app_job.start()] if next_app_job else app_tail
        build_cells = re.findall(r"- \{component_id: ([a-z]+)\}", app_block)
        if sorted(build_cells) != ["marketing", "mobile", "web"]:
            errors.append(
                f"application-build must schedule exactly the full-baseline build topology web/marketing/mobile, got {build_cells} (SC-FE-016)"
            )
    if host_runtime_start >= 0:
        runtime_cells = re.findall(r"- \{component_id: ([a-z]+)\}", host_block)
        if sorted(runtime_cells) != ["marketing", "web"]:
            errors.append(
                f"host-runtime must schedule exactly the full-baseline runtime topology web/marketing, got {runtime_cells} (SC-FE-016)"
            )
    if "resolve-frontend-ci-contract.mjs" not in frontend:
        errors.append("frontend workflow must resolve planner contracts via resolve-frontend-ci-contract.mjs (SC-FE-017)")
    if "steps.contract.outputs" not in frontend:
        errors.append("frontend contract resolution must flow through explicit step outputs (SC-FE-017)")

    backend = (workflows / "backend-ci.yml").read_text(encoding="utf-8")
    critical_backend_fqns = (
        "Notrelix.Architecture.Tests.DomainPurity.DomainBoundedContextSignatureTests",
        "Notrelix.Architecture.Tests.DomainPurity.DomainReferenceGraphTests",
        "Notrelix.Architecture.Tests.ApplicationLayer.HandlerDataPortGateTests",
        "Notrelix.Architecture.Tests.ApplicationLayer.HandlerConstructorPortGateTests",
        "Notrelix.Architecture.Tests.Pipeline.PipelineClosureArchitectureTests.PipelineClosure_CanonicalDocs_MatchFrozenSevenStageTopology",
        "Notrelix.Architecture.Tests.Pipeline.PipelineClosureArchitectureTests.PipelineClosure_LegacyGapCount_IsZero",
        "Notrelix.Architecture.Tests.Pipeline.PipelineClosureArchitectureTests.IntentionalAllowlistEntries_MustDescribePermanentArchitectureExceptions",
        "Notrelix.Architecture.Tests.Pipeline.PipelineClosureArchitectureTests.PipelineClosure_AllowlistEntries_AreNotStale",
        "Notrelix.Architecture.Tests.Pipeline.PipelineClosureArchitectureTests.PipelineClosure_AllProductionRequests_HaveValidDescriptors",
        "Notrelix.Architecture.Tests.CrossContextPersistenceBoundaryTests",
        "Notrelix.Architecture.Tests.CrossContextApplicationDependencyTests",
        "Notrelix.Architecture.Tests.DataAccess.CrossContextCascadeArchitectureTests",
        "Notrelix.Architecture.Tests.Contracts.PublicSemanticContractArchitectureTests",
        "Notrelix.Architecture.Tests.ApplicationLayer.ApplicationTransportBoundaryTests",
        "Notrelix.Architecture.Tests.Events.IntegrationEventOwnershipArchitectureTests",
        "Notrelix.Architecture.Tests.LayerRules.CommonEntitlementsAntiRegressionTests",
        "Notrelix.Architecture.Tests.FeatureStructureArchitectureTests",
        "Notrelix.Infrastructure.Tests.Data.Rls.RlsPolicyVerificationTests",
        "Notrelix.Infrastructure.Tests.Data.DomainEventInterceptorTests",
        "Notrelix.Platform.Tests.Messaging.Reliability.OrderingEnforcerTests",
        "Notrelix.Platform.Tests.Messaging.Reliability.PoisonDetectorTests",
        "Notrelix.Platform.Tests.Messaging.Consumers.ConsumerHostDeliveryContractTests",
        "Notrelix.API.Tests.Idempotency.IdempotencyEndpointContractTests",
        "Notrelix.Integration.Tests.Data.Ops.IdempotencyStoreIntegrationTests",
        "Notrelix.Integration.Tests.Integration.TenantIsolationTests",
        "Notrelix.Integration.Tests.Integration.CrossTenantIsolationTests",
        "Notrelix.Integration.Tests.Integration.RlsRuntimeEnforcementTests",
        "Notrelix.Integration.Tests.Auth.ApiTokenFlowTests",
        "Notrelix.Integration.Tests.Auth.ApiTokenHttpFlowTests",
        "Notrelix.Integration.Tests.Messaging.DeduplicationConsumeFilterIntegrationTests",
        "Notrelix.Integration.Tests.Messaging.DeduplicationConsumeFilterFullIntegrationTests",
        "Notrelix.Integration.Tests.Data.OutboxDispatchContractTests",
        "Notrelix.Integration.Tests.Data.OutboxClaimReclaimTests",
        "Notrelix.Integration.Tests.Data.OutboxAtomicityTests",
        "Notrelix.Integration.Tests.Data.MigrationSmokeTests",
        "Notrelix.Integration.Tests.Integration.ProductionCompositionTests",
        "Notrelix.Integration.Tests.Integration.Production.ProductionGraphTests",
        "Notrelix.Integration.Tests.Messaging.RealtimeResourceChangedConsumerIntegrationTests",
    )
    for fqn in critical_backend_fqns:
        if fqn in backend:
            errors.append(f"backend workflow retains critical FQN; move to backend/tests/ci-proofs.json: {fqn}")
    if "verify-required-proofs-trx.py" not in backend:
        errors.append("backend provider must verify registry proof profiles via verify-required-proofs-trx.py")
    if "backend/tests/ci-proofs.json" not in backend:
        errors.append("backend provider must consume the backend-owned proof registry")
    if not (root / "backend/tests/ci-proofs.json").exists():
        errors.append("backend internal proof registry backend/tests/ci-proofs.json missing")
    if re.search(r"redis:7-alpine", backend):
        errors.append("backend workflow must use the immutable locked redis ref (@sha256:), not a mutable tag")
    legacy_verifier = root / "scripts/ci/verify-required-tests-trx.py"
    if legacy_verifier.exists():
        errors.append("legacy FQN verifier scripts/ci/verify-required-tests-trx.py must be deleted after registry migration")
    for path in sorted(workflows.glob("*.yml")):
        if "verify-required-tests-trx.py" in path.read_text(encoding="utf-8"):
            errors.append(f"{path.name}: references retired legacy FQN verifier")
    migration_helper = root / "scripts/ci/check-migration-discipline.py"
    if migration_helper.exists():
        helper_text = migration_helper.read_text(encoding="utf-8")
        if re.search(r"GITHUB_EVENT|EVENT_NAME|github\.event", helper_text):
            errors.append("migration discipline helper must consume the resolved range, not interpret GitHub events")
    if "list package --vulnerable" in backend:
        errors.append("backend provider must not own dependency security; security-ci.yml is the single owner")

    security = (workflows / "security-ci.yml").read_text(encoding="utf-8")
    for required in ("check-dotnet-vulnerabilities.py", "check-pnpm-audit.mjs"):
        if required not in security:
            errors.append(f"security provider must use the structured checker {required}")

    # Startup-critical smoke environment: dropping any of these silently breaks
    # the backend container at Production startup validation (observed with
    # Cors:AllowedOrigins and Frontend:AppBaseUrl). Keep this list in sync with
    # the backend options validators.
    container_text = (workflows / "container-ci.yml").read_text(encoding="utf-8")
    for required_env in (
        "Cors__AllowedOrigins__0=",
        "Frontend__AppBaseUrl=",
        "Rls__Enabled=true",
        "Rls__SetSessionContext=true",
        "DataProtection__PersistKeys=true",
        "DataProtection__KeysPath=",
        "Messaging__Transport=RabbitMQ",
        "ConnectionStrings__NotrelixDb=",
        "ConnectionStrings__Redis=",
        "JwtSettings__SecretKey=",
    ):
        if required_env not in container_text:
            errors.append(f"backend runtime smoke lost startup-critical env: {required_env.rstrip('=')}")

    publish_start = container_text.find("\n  publish:\n")
    if publish_start < 0:
        errors.append("container provider must define a trusted-main publish job")
        outside_publish = container_text
    else:
        after_publish = container_text[publish_start + 1:]
        next_job = re.search(r"\n  [a-z][a-z0-9-]*:\n", after_publish)
        publish_end = len(container_text) if next_job is None else publish_start + 1 + next_job.start()
        publish_text = container_text[publish_start:publish_end]
        outside_publish = container_text.replace(publish_text, "", 1)
        if "docker build" in publish_text:
            errors.append("container publish must push the validated image bytes; a second build is forbidden")
        for write in ("packages: write", "id-token: write", "attestations: write"):
            if write in outside_publish:
                errors.append(f"container provider write permission {write!r} must be scoped to the publish job")
        if "release-image-" not in publish_text or 'kind:"ReleaseImage"' not in publish_text:
            errors.append("container publish must upload a ReleaseImage manifest binding component/source/plan/digest")

    for forbidden in ("publish_candidate", "docker push", "attest"):
        if forbidden in outside_publish:
            errors.append(f"container provider must not publish/attest outside the trusted publish job ({forbidden})")

    docs = (workflows / "docs-ci.yml").read_text(encoding="utf-8")
    if "make docs-check" not in docs:
        errors.append("docs provider must execute repository documentation governance")

    infra_workflow = (workflows / "infra-ci.yml").read_text(encoding="utf-8")
    if "python3 scripts/ci/validate-infra.py" not in infra_workflow:
        errors.append("infra provider must execute validate-infra.py")
    if "--runtime-images" in infra_workflow:
        errors.append("infra provider must not consume planner runtime contract")
    if "nginx_ref" not in infra_workflow:
        errors.append("infra provider must validate nginx syntax with the locked nginx_ref")
    if re.search(r"nginx:1\.27-alpine", infra_workflow):
        errors.append("infra workflow must use the immutable locked nginx ref (@sha256:), not a mutable tag")
    if "write-test-env.sh" not in infra_workflow or "deploy_env_var" not in infra_workflow:
        errors.append("infra provider must combine credentials env with resolved NOTRELIX_* runtime image env")

    infra_helper = (root / "scripts/ci/validate-infra.py").read_text(encoding="utf-8")
    if (
        "delivery_model" in infra_helper
        or "tools.deliveryctl" in infra_helper
        or "import tomllib" in infra_helper
        or "from tomllib" in infra_helper
    ):
        errors.append("validate-infra.py must consume resolved contracts, not delivery authority")
    if "--runtime-images" in infra_helper:
        errors.append("validate-infra.py must be standalone and must not accept planner runtime contract")

    makefile = root / "Makefile"
    if makefile.exists():
        make_text = makefile.read_text(encoding="utf-8")
        if re.search(
            r"(?ms)^(staging-up|prod-up|build-staging|build):.*?^\t.*docker compose.*(?:--build| build(?:\s|$))",
            make_text,
        ):
            errors.append("Makefile bypasses delivery platform with staging/production build")

    apply_script = root / "APPLY.sh"
    if apply_script.exists():
        apply_text = apply_script.read_text(encoding="utf-8")
        for forbidden in ("git push", "git commit", "gh pr", "gh api", "git update-ref"):
            if forbidden in apply_text:
                errors.append(f"APPLY.sh must be local-only; forbidden remote mutation token: {forbidden}")

    codeowners = root / ".github/CODEOWNERS"
    if not codeowners.exists():
        errors.append("CODEOWNERS missing")

    cd_text = "\n".join(
        (workflows / name).read_text(encoding="utf-8")
        for name in ("release.yml", "promote-release.yml", "deploy.yml")
    )
    if re.search(
        r"\bdocker\s+build\b|\bdocker\s+compose[^\n;&|]*\sbuild(?:\s|$)",
        cd_text,
    ):
        errors.append("release/CD rebuild forbidden")

    if errors:
        raise ValueError("\n".join(errors))
