# Freeze Governance Specification

> **Platform Freeze Policy, Baseline Audits, and Version Tagging**

---

## 1. Freeze Certificate Rules

1. **Clean Working Tree:** Freeze baseline audits require a clean git working tree (`git status --porcelain` is empty).
2. **HEAD Commit SHA:** Baseline certificates record the exact `HEAD` commit SHA.
3. **Mandatory Audit Gates:**
   - `TYPECHECK`
   - `LINT`
   - `TEST_NODE`
   - `TEST_WEB`
   - `CHECK_DEPS` / `ARCHITECTURE`
   - `BUILD`
   - `VALIDATE`

---

## 2. Post-Freeze Modification Policy

After `frontend-web-platform-v1.0.0` freeze tag is issued:
- Feature teams extend within their bounded context without modifying platform singletons.
- Modifications to `foundation/*`, `runtimes/web`, or `composition` require an Architectural Decision Record (ADR) and Tech Lead sign-off.
