export interface ReconnectPolicyConfig {
  readonly minDelayMs?: number;
  readonly maxDelayMs?: number;
  readonly maxRetries?: number;
  readonly backoffFactor?: number;
  readonly jitter?: boolean;
}

export class ReconnectPolicy {
  public readonly minDelayMs: number;
  public readonly maxDelayMs: number;
  public readonly maxRetries: number;
  public readonly backoffFactor: number;
  public readonly jitter: boolean;

  constructor(config: ReconnectPolicyConfig = {}) {
    this.minDelayMs = config.minDelayMs ?? 1000;
    this.maxDelayMs = config.maxDelayMs ?? 30000;
    this.maxRetries = config.maxRetries ?? 10;
    this.backoffFactor = config.backoffFactor ?? 2;
    this.jitter = config.jitter ?? true;
  }

  public shouldRetry(attempt: number): boolean {
    return attempt < this.maxRetries;
  }

  public getNextDelay(attempt: number): number {
    const rawDelay = this.minDelayMs * Math.pow(this.backoffFactor, Math.max(0, attempt));
    const cappedDelay = Math.min(this.maxDelayMs, rawDelay);

    if (!this.jitter) {
      return cappedDelay;
    }

    // Full jitter pattern: random delay between 0.5x and 1.5x of cappedDelay
    const jitterFactor = 0.5 + Math.random();
    return Math.floor(cappedDelay * jitterFactor);
  }
}
