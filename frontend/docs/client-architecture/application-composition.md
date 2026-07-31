# Application Composition Root Specification

> **Ownership and Lifecycle of System Services in `apps/web`**

---

## 1. Composition Root Responsibilities

The Composition Root in `apps/web/src/composition` is the single source of truth for instantiating long-lived system singletons:
1. **API Client:** `NotrelixClient` created with `createNotrelixClient`.
2. **Query Client:** `QueryClient` initialized with standard retry and failure policies.
3. **Realtime Client:** `RealtimeClient` initialized with browser WebSocket factory and state machine parameters.
4. **Application Services:** `WebApplicationServices` bundling feature repositories and domain service instances.

---

## 2. Service Container Interface

```ts
export interface WebApplicationServices {
  readonly runtime: AppRuntime;
  readonly queryClient: QueryClient;
  readonly workManagement: WorkManagementServices;
  readonly docs: DocsServices;
  readonly automation: AutomationServices;
  readonly features: FeatureServices;
  dispose(): Promise<void>;
}
```

---

## 3. Component Consumption Model

Components access application services using the `useApplicationServices()` hook provided by `ApplicationServicesProvider`:

```tsx
export function BoardViewContainer() {
  const { workManagement } = useApplicationServices();
  // Use repositories from workManagement
}
```
