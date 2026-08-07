import { describe, expect, it } from "vitest"
import type { NotrelixClient } from "@notrelix/contracts"
import { createWorkManagementServices } from "../services"

function createRecordingClient() {
  const calls: Array<{ method: string; url: string; body?: unknown }> = []
  const client = {
    api: {
      get: async <T>(url: string) => {
        calls.push({ method: "GET", url })
        return {} as T
      },
      post: async <T>(url: string, body?: unknown) => {
        calls.push({ method: "POST", url, body })
        return "" as T
      },
      put: async <T>(url: string, body?: unknown) => {
        calls.push({ method: "PUT", url, body })
        return undefined as T
      },
      patch: async <T>(url: string, body?: unknown) => {
        calls.push({ method: "PATCH", url, body })
        return undefined as T
      },
      delete: async <T>(url: string) => {
        calls.push({ method: "DELETE", url })
        return undefined as T
      },
    },
    endpoints: {} as NotrelixClient["endpoints"],
  } as NotrelixClient

  return { client, calls }
}

describe("createWorkManagementServices", () => {
  it("wires item movement to the injected API client", async () => {
    const { client, calls } = createRecordingClient()
    const services = createWorkManagementServices(client)

    await services.cards.moveCard({
      cardId: "card-1",
      listId: "group-2",
      position: 42,
    })

    expect(calls).toEqual([
      {
        method: "POST",
        url: "/cards/card-1/move",
        body: { listId: "group-2", position: 42 },
      },
    ])
  })
})
