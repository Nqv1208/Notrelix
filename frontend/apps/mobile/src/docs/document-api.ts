import type { BlockWireDto } from "@notrelix/docs-mobile";

/** The slice of `Documents` `PageDto` the reader needs. */
export interface PageWireDto {
  readonly id: string;
  readonly workspaceId: string;
  readonly title: string;
}

export interface DocumentRead {
  readonly page: PageWireDto;
  readonly blocks: readonly BlockWireDto[];
}

/** The producer refused or failed the read; `status` is its HTTP status. */
export class DocumentRequestError extends Error {
  constructor(readonly status: number) {
    super(`Document request failed with status ${status}`);
    this.name = "DocumentRequestError";
  }
}

/** A page the viewer cannot read looks the same as one that does not exist. */
export function isNoAccess(error: unknown): boolean {
  return (
    error instanceof DocumentRequestError &&
    (error.status === 401 || error.status === 403 || error.status === 404)
  );
}

export interface DocumentApiConfig {
  /** The API origin plus prefix, as the web client's `baseUrl`. */
  readonly baseUrl: string;
  readonly fetchImpl?: typeof fetch;
}

/**
 * Reads one page and its ordered blocks (`GET /pages/{id}`,
 * `GET /pages/{id}/blocks`). The mobile runtime owns no API client yet, so the
 * host does these two reads itself; the wire shapes are decoded by docs-mobile.
 */
export function createDocumentApi(config: DocumentApiConfig) {
  const fetchImpl = config.fetchImpl ?? globalThis.fetch.bind(globalThis);

  async function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
    const response = await fetchImpl(`${config.baseUrl}${path}`, {
      method: "GET",
      credentials: "include",
      headers: { Accept: "application/json" },
      signal,
    });
    if (!response.ok) throw new DocumentRequestError(response.status);
    return (await response.json()) as T;
  }

  return {
    async read(
      workspaceId: string,
      pageId: string,
      signal?: AbortSignal,
    ): Promise<DocumentRead> {
      const id = encodeURIComponent(pageId);
      const [page, blocks] = await Promise.all([
        getJson<PageWireDto>(`/pages/${id}`, signal),
        getJson<BlockWireDto[]>(`/pages/${id}/blocks`, signal),
      ]);
      // A page of another workspace is out of scope for this route.
      if (page.workspaceId !== workspaceId) throw new DocumentRequestError(404);
      return { page, blocks };
    },
  };
}
