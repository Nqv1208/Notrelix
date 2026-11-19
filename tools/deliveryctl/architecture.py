from __future__ import annotations

import re
from pathlib import Path

from .model import load_authorities
from .runtime import ROOT

SHA40 = re.compile(r"^[0-9a-f]{40}$")
USES = re.compile(r"^\s*uses:\s*([^\s#]+)", re.MULTILINE)
JOB_HEADER = re.compile(r"(?m)^  ([A-Za-z][A-Za-z0-9_-]*):\s*$")
LOCAL_ACTION_USE = "uses: ./.github/actions/"


def jobs_missing_local_action_checkout(text: str) -> list[str]:
    """SC-WF-001: a job consuming a local composite action must run
    actions/checkout earlier in the same job, otherwise the action does not
    exist on the runner and the step fails. Returns offending job names."""
    matches = list(JOB_HEADER.finditer(text))
    offenders: list[str] = []
    for index, match in enumerate(matches):
        end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
        body = text[match.start():end]
        use = body.find(LOCAL_ACTION_USE)
        if use >= 0 and "actions/checkout@" not in body[:use]:
            offenders.append(match.group(1))
    return offenders
PROVIDERS = {
    "container-ci.yml",
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

        for job in jobs_missing_local_action_checkout(text):
            errors.append(
                f"{path.name}: job {job} uses a local .github action without an earlier checkout (SC-WF-001)"
            )

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
                        path.name == "container-ci.yml"
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
        if not re.search(r"^  ci-gate:\s*$", ci_text, re.MULTILINE):
            errors.append("ci.yml must define the ci-gate closure job")

    for name in ("container-ci.yml", "security-ci.yml"):
        text = (workflows / name).read_text(encoding="utf-8")
        if "uses: ./.github/actions/emit-evidence" in text and "workflow_call:" not in text:
            errors.append(f"{name}: domain evidence requires reusable workflow_call entry")
        if "workflow_call:" in text and not re.search(r"inputs\.", text):
            errors.append(f"{name}: workflow_call entry must consume resolved inputs")

    backend = (workflows / "ci.yml").read_text(encoding="utf-8")
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
        "Rls__RoleEnforcement=Enforce",
        "ConnectionStrings__NotrelixDb=",
        "ConnectionStrings__NotrelixWorkerDb=",
        "ConnectionStrings__NotrelixMigrationDb=",
        "ConnectionStrings__Redis=",
        "JwtSettings__SecretKey=",
    ):
        if required_env not in container_text:
            errors.append(f"backend runtime smoke lost startup-critical env: {required_env.rstrip('=')}")

    if "Rls__RoleEnforcement=Warn" in container_text or "Rls__RoleEnforcement=Off" in container_text:
        errors.append("backend runtime smoke must run with Rls__RoleEnforcement=Enforce")
    for required_step in ("--migrate", "--rls-apply", "20-create-runtime-logins.sh"):
        if required_step not in container_text:
            errors.append(f"backend runtime smoke must prepare the database with {required_step}")

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
