/**
 * CSRF Protection Strategy for Notrelix
 *
 * For production hardening, the backend expects a CSRF token to prevent Cross-Site Request Forgery.
 * This helper reads the token from the standard `XSRF-TOKEN` cookie or a `<meta name="csrf-token">` tag.
 *
 * The API client automatically includes this token in the `X-XSRF-TOKEN` or `X-CSRF-TOKEN` header
 * for all non-GET, unsafe HTTP requests (POST, PUT, PATCH, DELETE).
 */

export function getCsrfToken(): string | null {
  if (typeof document === "undefined") {
    return null
  }

  // 1. Try reading from meta tag first
  const meta = document.querySelector('meta[name="csrf-token"]')
  if (meta) {
    const token = meta.getAttribute("content")
    if (token) return token
  }

  // 2. Try reading from XSRF-TOKEN cookie
  const match = document.cookie.match(/(?:^|; )XSRF-TOKEN=([^;]*)/)
  if (match) {
    return decodeURIComponent(match[1])
  }

  return null
}
