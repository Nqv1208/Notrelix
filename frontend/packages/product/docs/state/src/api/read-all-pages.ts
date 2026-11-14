import type { DocsApiClient } from "./page.api";

/** One cursor page as the API answers it (`CursorPage<T>`). */
export interface CursorPageDtoApi<T> {
  items?: T[] | null;
  /** Opaque; sent back as `cursor` for the next page, absent or null on the last one. */
  nextCursor?: string | null;
}

/**
 * Reads a cursor-paged list to the end: follows `nextCursor` until it runs
 * out and returns every item in the order the server gave them.
 */
export async function readAllCursorPages<T>(
  api: DocsApiClient,
  url: string,
): Promise<T[]> {
  const items: T[] = [];
  let cursor: string | null | undefined;
  do {
    const next = cursor
      ? `${url}${url.includes("?") ? "&" : "?"}cursor=${encodeURIComponent(cursor)}`
      : url;
    const body = await api.get<CursorPageDtoApi<T>>(next);
    items.push(...(body.items ?? []));
    cursor = body.nextCursor;
  } while (cursor);
  return items;
}
