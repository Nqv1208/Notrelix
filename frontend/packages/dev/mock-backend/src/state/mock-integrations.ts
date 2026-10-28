/**
 * The workspace's integration connections as the producer serves them
 * (`Integrations.Connections.*`, `Integrations.Webhooks.Connect`): the catalog
 * with whether each provider is configured, the connections with their status,
 * the member who connected them and the last success and error, and the
 * actions on them. Google Calendar is "not configured" here, the way a backend
 * without a client id and secret reports it; Slack is configured.
 */
export type ConnectionStatus = "active" | "paused" | "error";

export interface StoredConnection {
  readonly id: string;
  readonly provider: "google-calendar" | "slack" | "webhook";
  status: ConnectionStatus;
  readonly connectedBy: string;
  readonly connectedAt: string;
  lastSuccessAt: string | null;
  lastErrorAt: string | null;
  lastError: string | null;
  readonly targetUrl: string | null;
}

export const INTEGRATION_CATALOG = [
  { provider: "google-calendar", configured: false },
  { provider: "slack", configured: true },
  { provider: "webhook", configured: true },
] as const;

export class MockIntegrations {
  private connections: StoredConnection[] = [];
  private sequence = 0;

  reset(): void {
    this.connections = [];
    this.sequence = 0;
  }

  list(): readonly StoredConnection[] {
    return this.connections;
  }

  find(id: string): StoredConnection | undefined {
    return this.connections.find((connection) => connection.id === id);
  }

  add(
    provider: StoredConnection["provider"],
    connectedBy: string,
    connectedAt: string,
    targetUrl: string | null,
  ): StoredConnection {
    this.sequence += 1;
    const connection: StoredConnection = {
      id: `00000000-0000-7000-8000-${String(this.sequence).padStart(12, "0")}`,
      provider,
      status: "active",
      connectedBy,
      connectedAt,
      lastSuccessAt: null,
      lastErrorAt: null,
      lastError: null,
      targetUrl,
    };
    this.connections.push(connection);
    return connection;
  }

  remove(id: string): boolean {
    const before = this.connections.length;
    this.connections = this.connections.filter(
      (connection) => connection.id !== id,
    );
    return this.connections.length < before;
  }
}
