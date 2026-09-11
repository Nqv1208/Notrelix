from __future__ import annotations

import re
from pathlib import Path

from .model import load_authorities
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
                    # W4 transition exception: the backend standalone lane (until
                    # the W7 cutover) has no plan to receive redis_image from, so
                    # it single-sources the locked digest once as FALLBACK_REDIS_REF.
                    # Remove together with the standalone lane; any second
                    # occurrence or use outside backend-ci.yml stays forbidden.
                    transitional_fallback = (
                        path.name == "backend-ci.yml"
                        and image_id == "redis"
                        and text.count(ref) == 1
                        and "FALLBACK_REDIS_REF" in text
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
    if "has the following vulnerable packages" not in backend:
        errors.append("backend must fail on vulnerable NuGet packages")

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

    for forbidden in ("publish_candidate", "docker push", "attest"):
        if forbidden in container_text:
            errors.append(f"container provider must not publish/attest images ({forbidden})")

    docs = (workflows / "docs-ci.yml").read_text(encoding="utf-8")
    if "make docs-check" not in docs:
        errors.append("docs provider must execute repository documentation governance")

    infra_workflow = (workflows / "infra-ci.yml").read_text(encoding="utf-8")
    if "python3 scripts/ci/validate-infra.py" not in infra_workflow:
        errors.append("infra provider must execute validate-infra.py")
    if "--runtime-images" in infra_workflow:
        errors.append("infra provider must not consume planner runtime contract")

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
