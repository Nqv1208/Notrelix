/**
 * Transport for a multipart upload with progress and abort.
 *
 * `fetch` reports no upload progress, so a browser upload runs over
 * `XMLHttpRequest`. A client built with an injected `fetchImpl` (the mock
 * backend, tests) has no XHR to reach and sends through that `fetch` instead.
 * Both resolve to the same raw shape; status handling stays in the client.
 */

export interface UploadProgress {
  readonly loaded: number;
  readonly total: number;
}

export interface RawUploadResponse {
  readonly status: number;
  readonly bodyText: string;
}

export interface UploadTransportRequest {
  readonly url: string;
  /** `POST` unless the operation replaces a resource (`PUT`). */
  readonly method: "POST" | "PUT";
  readonly form: FormData;
  /** No Content-Type: the transport sets the multipart boundary itself. */
  readonly headers: Readonly<Record<string, string>>;
  readonly signal?: AbortSignal | undefined;
  readonly onProgress?: ((progress: UploadProgress) => void) | undefined;
}

export type UploadTransport = (
  request: UploadTransportRequest,
) => Promise<RawUploadResponse>;

/** Thrown by a transport when the caller aborted. */
export class UploadAbortedError extends Error {
  constructor() {
    super("Upload was aborted.");
    this.name = "AbortError";
  }
}

export function createXhrUploadTransport(
  createXhr: () => XMLHttpRequest,
): UploadTransport {
  return (request) =>
    new Promise<RawUploadResponse>((resolve, reject) => {
      if (request.signal?.aborted) {
        reject(new UploadAbortedError());
        return;
      }
      const xhr = createXhr();
      xhr.open(request.method, request.url);
      xhr.withCredentials = true;
      for (const [name, value] of Object.entries(request.headers)) {
        xhr.setRequestHeader(name, value);
      }
      const onAbort = () => xhr.abort();
      request.signal?.addEventListener("abort", onAbort, { once: true });
      const done = () => request.signal?.removeEventListener("abort", onAbort);
      xhr.upload.onprogress = (event) => {
        if (event.lengthComputable) {
          request.onProgress?.({ loaded: event.loaded, total: event.total });
        }
      };
      xhr.onload = () => {
        done();
        resolve({ status: xhr.status, bodyText: xhr.responseText ?? "" });
      };
      xhr.onerror = () => {
        done();
        reject(new TypeError("Network error during upload."));
      };
      xhr.ontimeout = xhr.onerror;
      xhr.onabort = () => {
        done();
        reject(new UploadAbortedError());
      };
      xhr.send(request.form);
    });
}

export function createFetchUploadTransport(
  fetchImpl: typeof fetch,
): UploadTransport {
  return async (request) => {
    const init: RequestInit = {
      method: request.method,
      body: request.form,
      headers: { ...request.headers },
      credentials: "include",
    };
    if (request.signal) init.signal = request.signal;
    const response = await fetchImpl(request.url, init);
    return { status: response.status, bodyText: await response.text() };
  };
}
