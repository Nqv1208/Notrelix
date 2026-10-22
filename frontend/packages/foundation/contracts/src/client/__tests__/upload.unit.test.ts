import { describe, expect, it, vi } from "vitest";
import { AppError } from "@notrelix/kernel";
import { createNotrelixClient } from "../api-client";

/** A scripted XMLHttpRequest: the test drives progress and the answer. */
class FakeXhr {
  static instances: FakeXhr[] = [];
  method = "";
  url = "";
  withCredentials = false;
  headers: Record<string, string> = {};
  body: unknown = null;
  status = 0;
  responseText = "";
  aborted = false;
  upload: { onprogress: ((event: unknown) => void) | null } = {
    onprogress: null,
  };
  onload: (() => void) | null = null;
  onerror: (() => void) | null = null;
  ontimeout: (() => void) | null = null;
  onabort: (() => void) | null = null;

  constructor() {
    FakeXhr.instances.push(this);
  }
  open(method: string, url: string) {
    this.method = method;
    this.url = url;
  }
  setRequestHeader(name: string, value: string) {
    this.headers[name] = value;
  }
  send(body: unknown) {
    this.body = body;
  }
  abort() {
    this.aborted = true;
    this.onabort?.();
  }
  progress(loaded: number, total: number) {
    this.upload.onprogress?.({ lengthComputable: true, loaded, total });
  }
  answer(status: number, body: unknown) {
    this.status = status;
    this.responseText = body === null ? "" : JSON.stringify(body);
    this.onload?.();
  }
}

const URL_OF_UPLOAD = "/board-items/item-1/attachments/files";

function setup(csrfTokens: string[] = ["csrf-1"]) {
  FakeXhr.instances = [];
  const tokens = [...csrfTokens];
  const fetchImpl = vi.fn(async (url: string) => {
    if (String(url).includes("/auth/csrf")) {
      return new Response(
        JSON.stringify({ token: tokens.shift() ?? "csrf-x" }),
        {
          status: 200,
        },
      );
    }
    return new Response(null, { status: 204 });
  });
  const client = createNotrelixClient({
    baseUrl: "http://api.test",
    fetchImpl: fetchImpl as unknown as typeof fetch,
    createXhr: () => new FakeXhr() as unknown as XMLHttpRequest,
    createCorrelationId: () => "corr-1",
  });
  return { client, fetchImpl };
}

const form = () => {
  const data = new FormData();
  data.append("file", new File(["hello"], "ghi-chu.pdf"), "ghi-chu.pdf");
  return data;
};

/** Lets the client reach `xhr.send` (the CSRF bootstrap is awaited first). */
async function sent(index = 0): Promise<FakeXhr> {
  await vi.waitFor(() => {
    expect(FakeXhr.instances[index]).toBeDefined();
    expect(FakeXhr.instances[index]!.body).not.toBeNull();
  });
  return FakeXhr.instances[index]!;
}

describe("api.upload (multipart over XHR)", () => {
  it("posts the FormData with credentials, CSRF, idempotency and correlation headers and no JSON content type", async () => {
    const { client } = setup();
    const body = form();
    const result = client.api.upload<{ id: string }>(URL_OF_UPLOAD, body, {
      idempotencyKey: "key-1",
    });
    const xhr = await sent();
    expect(xhr.method).toBe("POST");
    expect(xhr.url).toBe(`http://api.test${URL_OF_UPLOAD}`);
    expect(xhr.withCredentials).toBe(true);
    expect(xhr.headers).toEqual({
      "X-Correlation-ID": "corr-1",
      "Idempotency-Key": "key-1",
      "X-CSRF-Token": "csrf-1",
    });
    expect(xhr.body).toBe(body);
    const part = (xhr.body as FormData).get("file") as File;
    expect(part.name).toBe("ghi-chu.pdf");
    xhr.answer(201, { id: "att-1", kind: "File" });
    await expect(result).resolves.toEqual({ id: "att-1", kind: "File" });
  });

  it("reports upload progress", async () => {
    const { client } = setup();
    const onProgress = vi.fn();
    const result = client.api.upload(URL_OF_UPLOAD, form(), {
      idempotencyKey: "key-1",
      onProgress,
    });
    const xhr = await sent();
    xhr.progress(5, 10);
    xhr.progress(10, 10);
    xhr.answer(201, {});
    await result;
    expect(onProgress.mock.calls).toEqual([
      [{ loaded: 5, total: 10 }],
      [{ loaded: 10, total: 10 }],
    ]);
  });

  it.each([
    [400, undefined, "validation"],
    [403, undefined, "forbidden"],
    [404, undefined, "not_found"],
    [409, "attachment.limit_exceeded", "conflict"],
    [413, "attachment.payload_too_large", "unknown"],
    [415, "attachment.unsupported_media_type", "unknown"],
  ])(
    "maps a %i answer to an AppError carrying its errorCode",
    async (status, errorCode, kind) => {
      const { client } = setup();
      const result = client.api.upload(URL_OF_UPLOAD, form(), {
        idempotencyKey: "key-1",
      });
      const caught = result.catch((error: unknown) => error);
      const xhr = await sent();
      xhr.answer(status, { title: "refused", errorCode });
      const error = (await caught) as AppError;
      expect(error).toBeInstanceOf(AppError);
      expect(error.status).toBe(status);
      expect(error.kind).toBe(kind);
      expect(error.code).toBe(errorCode);
    },
  );

  it("maps a broken connection to a network error", async () => {
    const { client } = setup();
    const caught = client.api
      .upload(URL_OF_UPLOAD, form(), { idempotencyKey: "key-1" })
      .catch((error: unknown) => error);
    const xhr = await sent();
    xhr.onerror?.();
    expect(((await caught) as AppError).kind).toBe("network");
  });

  it("aborts the request when the signal fires and reports an aborted error", async () => {
    const { client } = setup();
    const controller = new AbortController();
    const caught = client.api
      .upload(URL_OF_UPLOAD, form(), {
        idempotencyKey: "key-1",
        signal: controller.signal,
      })
      .catch((error: unknown) => error);
    const xhr = await sent();
    controller.abort();
    expect(xhr.aborted).toBe(true);
    expect(((await caught) as AppError).kind).toBe("aborted");
  });

  it("does not send when the signal is already aborted", async () => {
    const { client } = setup();
    const controller = new AbortController();
    controller.abort();
    const caught = await client.api
      .upload(URL_OF_UPLOAD, form(), {
        idempotencyKey: "key-1",
        signal: controller.signal,
      })
      .catch((error: unknown) => error);
    expect((caught as AppError).kind).toBe("aborted");
    expect(FakeXhr.instances[0]?.body ?? null).toBeNull();
  });

  it("re-bootstraps the CSRF token once on a CSRF 403 and resends with the same idempotency key", async () => {
    const { client } = setup(["stale", "fresh"]);
    const result = client.api.upload(URL_OF_UPLOAD, form(), {
      idempotencyKey: "key-1",
    });
    const first = await sent(0);
    expect(first.headers["X-CSRF-Token"]).toBe("stale");
    first.answer(403, { errorCode: "security.csrf_validation_failed" });
    const second = await sent(1);
    expect(second.headers["X-CSRF-Token"]).toBe("fresh");
    expect(second.headers["Idempotency-Key"]).toBe("key-1");
    second.answer(201, { id: "att-1" });
    await expect(result).resolves.toEqual({ id: "att-1" });
    expect(FakeXhr.instances).toHaveLength(2);
  });

  it("gives up after one CSRF recovery", async () => {
    const { client } = setup(["a", "b", "c"]);
    const caught = client.api
      .upload(URL_OF_UPLOAD, form(), { idempotencyKey: "key-1" })
      .catch((error: unknown) => error);
    (await sent(0)).answer(403, {
      errorCode: "security.csrf_validation_failed",
    });
    (await sent(1)).answer(403, {
      errorCode: "security.csrf_validation_failed",
    });
    expect(((await caught) as AppError).status).toBe(403);
    expect(FakeXhr.instances).toHaveLength(2);
  });

  it("a retry by the caller sends the key it is given: the same key replays the same action", async () => {
    const { client } = setup();
    const first = client.api
      .upload(URL_OF_UPLOAD, form(), { idempotencyKey: "action-1" })
      .catch((error: unknown) => error);
    (await sent(0)).answer(503, null);
    await first;
    const second = client.api.upload(URL_OF_UPLOAD, form(), {
      idempotencyKey: "action-1",
    });
    const retried = await sent(1);
    expect(retried.headers["Idempotency-Key"]).toBe("action-1");
    retried.answer(201, { id: "att-1" });
    await second;
  });
});

describe("api.upload through an injected fetch (no XHR)", () => {
  it("sends the FormData without a JSON content type", async () => {
    const calls: Array<{ url: string; init: RequestInit }> = [];
    const fetchImpl = vi.fn(async (url: string, init: RequestInit = {}) => {
      if (String(url).includes("/auth/csrf"))
        return new Response(JSON.stringify({ token: "csrf-1" }), {
          status: 200,
        });
      calls.push({ url, init });
      return new Response(JSON.stringify({ id: "att-1" }), { status: 201 });
    });
    const client = createNotrelixClient({
      baseUrl: "http://api.test",
      fetchImpl: fetchImpl as unknown as typeof fetch,
    });
    const body = form();
    await expect(
      client.api.upload(URL_OF_UPLOAD, body, { idempotencyKey: "key-1" }),
    ).resolves.toEqual({ id: "att-1" });
    const { init } = calls[0]!;
    expect(init.body).toBe(body);
    expect(init.credentials).toBe("include");
    const headers = init.headers as Record<string, string>;
    expect(headers["Content-Type"]).toBeUndefined();
    expect(headers["Idempotency-Key"]).toBe("key-1");
    expect(headers["X-CSRF-Token"]).toBe("csrf-1");
  });
});

describe("api.getBlob", () => {
  it("reads a file with the session and returns its bytes", async () => {
    const fetchImpl = vi.fn(async () => new Response("bytes", { status: 200 }));
    const client = createNotrelixClient({
      baseUrl: "http://api.test",
      fetchImpl: fetchImpl as unknown as typeof fetch,
    });
    const blob = await client.api.getBlob(
      "/board-items/i/attachments/a/content",
    );
    expect(await blob.text()).toBe("bytes");
    expect(fetchImpl).toHaveBeenCalledWith(
      "http://api.test/board-items/i/attachments/a/content",
      expect.objectContaining({ method: "GET", credentials: "include" }),
    );
  });

  it("maps a 404 to a not_found error", async () => {
    const fetchImpl = vi.fn(
      async () =>
        new Response(JSON.stringify({ title: "gone" }), { status: 404 }),
    );
    const client = createNotrelixClient({
      baseUrl: "http://api.test",
      fetchImpl: fetchImpl as unknown as typeof fetch,
    });
    await expect(
      client.api.getBlob("/board-items/i/attachments/a/content"),
    ).rejects.toMatchObject({ kind: "not_found", status: 404 });
  });
});
