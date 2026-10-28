/**
 * Where the API lives, so a path the API returns for an image (a profile
 * photo is `/api/v1/users/{id}/avatar?v=...`) can be shown by an `<img>`
 * even when the web app and the API are different origins. The client sets
 * it from its `baseUrl`; this is the one place a returned path becomes an
 * address.
 */
let apiOrigin: string | null = null;

/** Called by the client with its base URL; a relative base means same origin. */
export function configureApiAssetOrigin(baseUrl: string): void {
  try {
    apiOrigin = new URL(baseUrl).origin;
  } catch {
    apiOrigin = null;
  }
}

/**
 * An address an `<img>` can load: a path starting with `/` is placed on the
 * API origin; an absolute URL, a `data:`/`blob:` URL and `null` are kept.
 */
export function resolveApiAssetUrl(
  url: string | null | undefined,
): string | null {
  if (!url) return null;
  if (apiOrigin && url.startsWith("/") && !url.startsWith("//"))
    return `${apiOrigin}${url}`;
  return url;
}
