# Open Quality & Architectural Gates

The following gates must be 100% satisfied before creating tag `frontend-web-platform-v1.0.0`:

1. **FE-FZ-01 Architecture Enforcement:** AST, manifest and folder boundary checks close documented blind spots with zero temporary bypass at freeze.
2. **FE-FZ-02 Contracts:** OpenAPI/realtime contract generation is deterministic, generated exports are public and at least one production vertical slice adopts generated operation types.
   - Current status: frontend generator work is complete; backend export/provenance is tracked in `../issues/fe-fz-02-backend-contract-export-provenance.md`.
3. **FE-FZ-03 Composition:** ApplicationServices is the only production path for module repositories; fake API/repository singletons are removed.
   - Current status: Work Management production consumers use injected services from application composition; remaining checker failures are later gates.
4. **FE-FZ-04 to FE-FZ-09 Runtime:** kernel/platform, runtime lifecycle, router guards, realtime protocol/orchestration and optimistic command conventions are frozen.
   - Current status: FE-FZ-04 and FE-FZ-05 frontend runtime work is complete; FE-FZ-06 and FE-FZ-07 have backend/API or realtime blockers tracked in `../issues/`.
5. **FE-FZ-10 to FE-FZ-13 Module Admission:** Work Management, Documents, Automation and feature modules meet their admission gates before module-scale development.
6. **FE-FZ-14 to FE-FZ-16 Support Gates:** UI, observability, production preview and critical E2E gates pass.
7. **FE-FZ-17 Certificate:** Freeze audit uses HEAD, requires a clean working tree and writes an immutable certificate only after all gates pass.
