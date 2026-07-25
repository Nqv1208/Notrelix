export type RealtimeConnectionState = 'connecting' | 'connected' | 'reconnecting' | 'disconnected' | 'failed';

export interface RealtimeEnvelope<TPayload = unknown> {
  eventId: string;
  eventType: string;
  tenantId?: string;
  workspaceId: string;
  aggregateId?: string;
  timestamp: string;
  payload: TPayload;
}

export type RealtimeEvent = RealtimeEnvelope<unknown>;

export type ConnectionStateListener = (state: RealtimeConnectionState) => void;
export type EventListener<T = unknown> = (envelope: RealtimeEnvelope<T>) => void;

export class RealtimeClient {
  private socket: WebSocket | null = null;
  private state: RealtimeConnectionState = 'disconnected';
  private eventListeners: Set<EventListener> = new Set();
  private stateListeners: Set<ConnectionStateListener> = new Set();
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private baseReconnectDelayMs = 1000;
  private reconnectTimer: ReturnType<typeof setTimeout> | null = null;

  constructor(private url: string) {}

  public getState(): RealtimeConnectionState {
    return this.state;
  }

  private setState(newState: RealtimeConnectionState) {
    if (this.state !== newState) {
      this.state = newState;
      this.stateListeners.forEach((listener) => listener(newState));
    }
  }

  public connect() {
    if (this.state === 'connected' || this.state === 'connecting') return;

    this.setState(this.reconnectAttempts > 0 ? 'reconnecting' : 'connecting');

    try {
      this.socket = new WebSocket(this.url);

      this.socket.onopen = () => {
        this.reconnectAttempts = 0;
        this.setState('connected');
      };

      this.socket.onmessage = (event) => {
        try {
          const envelope: RealtimeEnvelope = JSON.parse(event.data);
          this.eventListeners.forEach((listener) => listener(envelope));
        } catch (err) {
          console.error('[RealtimeClient] Failed to parse message:', err);
        }
      };

      this.socket.onclose = () => {
        this.handleDisconnect();
      };

      this.socket.onerror = (error) => {
        console.error('[RealtimeClient] WebSocket error:', error);
      };
    } catch (e) {
      console.error('[RealtimeClient] Connection error:', e);
      this.handleDisconnect();
    }
  }

  private handleDisconnect() {
    this.socket = null;
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;
      const delay = Math.min(this.baseReconnectDelayMs * Math.pow(2, this.reconnectAttempts - 1), 30000);
      this.setState('reconnecting');
      if (this.reconnectTimer) clearTimeout(this.reconnectTimer);
      this.reconnectTimer = setTimeout(() => this.connect(), delay);
    } else {
      this.setState('failed');
    }
  }

  public subscribe<T = unknown>(listener: EventListener<T>) {
    this.eventListeners.add(listener as EventListener);
    return () => {
      this.eventListeners.delete(listener as EventListener);
    };
  }

  public onStateChange(listener: ConnectionStateListener) {
    this.stateListeners.add(listener);
    return () => {
      this.stateListeners.delete(listener);
    };
  }

  public disconnect() {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }
    this.reconnectAttempts = 0;
    if (this.socket) {
      this.socket.close();
      this.socket = null;
    }
    this.setState('disconnected');
  }
}

export const DEFAULT_REALTIME_URL = 'wss://api.notrelix.com/stream';
export const realtimeClient = new RealtimeClient(DEFAULT_REALTIME_URL);
