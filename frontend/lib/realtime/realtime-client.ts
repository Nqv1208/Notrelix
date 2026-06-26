// Realtime WebSocket client contract for SSE and synchronization events.

export type RealtimeEvent = {
  type: string
  payload: unknown
}

export class RealtimeClient {
  private socket: WebSocket | null = null
  private listeners: Set<(event: RealtimeEvent) => void> = new Set()

  constructor(private url: string) {}

  connect() {
    try {
      this.socket = new WebSocket(this.url)
      this.socket.onmessage = (event) => {
        const data: RealtimeEvent = JSON.parse(event.data)
        this.listeners.forEach((listener) => listener(data))
      }
      this.socket.onclose = () => {
        setTimeout(() => this.connect(), 5000) // reconnect backoff
      }
    } catch (e) {
      console.error("Failed to connect to realtime stream:", e)
    }
  }

  subscribe(listener: (event: RealtimeEvent) => void) {
    this.listeners.add(listener)
    return () => {
      this.listeners.delete(listener)
    }
  }

  disconnect() {
    if (this.socket) {
      this.socket.close()
    }
  }
}

export const realtimeClient = new RealtimeClient(
  process.env.NEXT_PUBLIC_WS_URL || "wss://api.notrelix.com/stream"
)
