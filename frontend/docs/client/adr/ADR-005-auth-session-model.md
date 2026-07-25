# ADR-005: Auth Session Model

**Date:** 2026-07-09  
**Status:** Accepted  

## Context

The Notrelix client app must authenticate requests to the ASP.NET Core backend. 
- The backend utilizes a secure **cookie-based session** authentication mechanism (HttpOnly cookies) rather than custom Bearer tokens managed in JavaScript.
- Requests require Cross-Site Request Forgery (CSRF) protection via `X-XSRF-TOKEN` or `X-CSRF-TOKEN` headers for unsafe HTTP methods (POST, PUT, PATCH, DELETE).
- When a session expires, a 401 response initiates a token refresh flow via `POST /auth/refresh`.
- If refresh fails, the user must be redirected to sign in.

We need to define the boundaries, state management, and navigation rules for authentication in the frontend.

## Decision

We decide on the following architecture for authentication and session management:

1. **No In-Memory Access Token Storage**: The frontend will not store, read, or manage access tokens in memory or local storage. All HTTP requests made by `@notrelix/contracts` will use `credentials: "include"` to automatically leverage native browser cookies.
2. **Decoupled Navigation (Callback Pattern — Rule R2)**:
   - Feature packages like `@notrelix/features-auth` must remain platform-agnostic and router-independent. They must not depend on `apps/web` navigation libraries (like TanStack Router) or hard-code route paths (like `/sign-in`).
   - The `AuthProvider` component in `@notrelix/features-auth` will accept an `onAuthFailure` callback prop:
     ```tsx
     interface AuthProviderProps {
       children: React.ReactNode;
       onAuthFailure: (currentPath: string) => void;
     }
     ```
   - `apps/web` is responsible for providing the router logic to this callback (e.g., navigating to `/sign-in?redirect=currentPath`).
3. **Decoupled API Client and Auth Interceptors**:
   - The token refresh interceptor, CSRF header injection, and global HTTP client are situated in `@notrelix/contracts/client` and `@notrelix/platform/src/auth`.
   - `@notrelix/runtime-web` remains strictly a set of browser API adapters (localStorage, sessionStorage, broadcast channel, clipboard, navigator status) and will **not** contain HTTP client wrappers, token injection, or refresh interceptors.
4. **Auth Failure Event Flow**:
   - When `apiFetch` encounters a refresh token failure, it dispatches a global `CustomEvent("auth:failure")`.
   - The `AuthProvider` hook listens to this event and triggers the injected `onAuthFailure` callback to cleanly handle session expiration.

## Consequences

- **Secure by Default**: Session tokens are HttpOnly and not accessible via JavaScript, mitigating XSS risks.
- **Portability**: `@notrelix/features-auth` is reusable on mobile (`apps/mobile`) and web (`apps/web`) because it does not assume routing structures.
- **Clear Boundaries**: `runtime-web` is kept thin and simple. Network adapters/clients are located in `@notrelix/contracts`.
