# FE-FZ-14 Issue: UI Foundation Freeze Remaining Work

## Scope

Frontend completed the small FE-FZ-14 items that could be implemented without introducing a new design/test stack:

- exported `ForbiddenState`
- added `UpgradeRequiredState`
- added `SubmitState`
- exported `mapServerValidationErrors`
- kept `ThemeProvider` in `@notrelix/ui-web`

## Remaining Work

### 1. Storybook CI is not production-ready

`tooling/storybook/web` exists, but FE-FZ-14 requires a real static Storybook build gate for critical primitives. The current workspace does not include enough Storybook config/stories to prove:

- Button/Input/Dialog/Popover/Menu/Tooltip/Select/Combobox behavior
- feedback states
- form conventions
- light/dark/system theme behavior
- density variants

### 2. Accessibility tests are missing

FE-FZ-14 requires axe/keyboard/focus/ARIA checks for critical primitives. No dedicated primitive accessibility test suite is currently wired.

### 3. Critical visual smoke is missing

There is no visual smoke gate for UI primitives. Snapshot-everything is not required, but critical primitives still need a browser-backed smoke suite.

### 4. Density contract is incomplete

Board table density is referenced in the plan, but the UI token package does not yet expose a complete density contract for board/table components.

### 5. Primitive coverage needs audit

Most primitives exist in `@notrelix/ui-web`, but FE-FZ-14 explicitly calls out Combobox and Toast conventions. Combobox is currently represented through lower-level command/popover primitives, and toast remains tied to `sonner`.

## Acceptance Criteria

- `storybook-build` gate exists and runs in frontend CI.
- Critical primitive stories exist and cover light/dark/system.
- Axe/keyboard/focus tests cover the critical primitive set.
- Board table density tokens are documented and exported.
- Toast/Combobox conventions are formalized in `@notrelix/ui-web`.
