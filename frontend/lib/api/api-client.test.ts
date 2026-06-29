import { describe, test, expect, mock, beforeEach, afterEach } from "bun:test"
import { api } from "./api-client"
import { AppError } from "@/lib/errors/app-error"

describe("api-client", () => {
  let originalFetch: typeof fetch

  beforeEach(() => {
    originalFetch = global.fetch
  })

  afterEach(() => {
    global.fetch = originalFetch
  })

  test("handles 204 No Content correctly", async () => {
    global.fetch = mock(() =>
      Promise.resolve(
        new Response("", {
          status: 204,
          statusText: "No Content",
        })
      )
    )

    const result = await api.get("/test-204")
    expect(result).toBeNull()
  })

  test("handles network failures by throwing a network AppError", async () => {
    global.fetch = mock(() => Promise.reject(new TypeError("Failed to fetch")))

    try {
      await api.get("/test-fail")
      expect().fail("Should have thrown an error")
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } catch (error: any) {
      expect(error).toBeInstanceOf(AppError)
      expect(error.kind).toBe("network")
      expect(error.message).toContain("Network error")
    }
  })

  test("handles aborted requests by throwing an aborted AppError", async () => {
    const abortError = new DOMException("The user aborted a request.", "AbortError")
    global.fetch = mock(() => Promise.reject(abortError))

    try {
      await api.get("/test-abort", { signal: new AbortController().signal })
      expect().fail("Should have thrown an error")
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } catch (error: any) {
      expect(error).toBeInstanceOf(AppError)
      expect(error.kind).toBe("aborted")
      expect(error.message).toContain("aborted")
    }
  })

  test("handles validation errors (400) and maps them to validation AppError", async () => {
    const errorBody = {
      message: "Invalid inputs",
      errors: {
        email: ["Email is invalid"],
      },
    }

    global.fetch = mock(() =>
      Promise.resolve(
        new Response(JSON.stringify(errorBody), {
          status: 400,
          headers: { "Content-Type": "application/json" },
        })
      )
    )

    try {
      await api.post("/test-validation", { email: "invalid" })
      expect().fail("Should have thrown an error")
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } catch (error: any) {
      expect(error).toBeInstanceOf(AppError)
      expect(error.kind).toBe("validation")
      expect(error.message).toBe("Invalid inputs")
      expect(error.validationErrors).toBeDefined()
      expect(error.validationErrors?.email).toEqual(["Email is invalid"])
    }
  })

  test("performs token refresh on 401 and retries original request once", async () => {
    let callCount = 0
    let refreshCalled = false

    global.fetch = mock((input) => {
      const url = typeof input === "string" ? input : (input as Request).url
      callCount++

      if (url.includes("/auth/refresh")) {
        refreshCalled = true
        return Promise.resolve(new Response(JSON.stringify({ ok: true }), { status: 200 }))
      }

      if (callCount === 1) {
        return Promise.resolve(new Response("Unauthorized", { status: 401 }))
      }

      return Promise.resolve(new Response(JSON.stringify({ data: "success" }), { status: 200 }))
    })

    const result = await api.get<{ data: string }>("/test-retry")
    expect(result.data).toBe("success")
    expect(refreshCalled).toBe(true)
    expect(callCount).toBe(3) // 1st try (401) + refresh (200) + 2nd try (200)
  })

  test("fails and dispatches auth:failure if refresh fails with 401", async () => {
    let eventDispatched = false

    if (typeof window !== "undefined") {
      window.addEventListener("auth:failure", () => {
        eventDispatched = true
      })
    }

    global.fetch = mock((input) => {
      const url = typeof input === "string" ? input : (input as Request).url

      if (url.includes("/auth/refresh")) {
        return Promise.resolve(new Response("Refresh Token Expired", { status: 401 }))
      }

      return Promise.resolve(new Response("Unauthorized", { status: 401 }))
    })

    try {
      await api.get("/test-refresh-fail")
      expect().fail("Should have thrown an error")
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    } catch (error: any) {
      expect(error).toBeInstanceOf(AppError)
      expect(error.kind).toBe("auth")
      expect(error.status).toBe(401)
      if (typeof window !== "undefined") {
        expect(eventDispatched).toBe(true)
      }
    }
  })
})
