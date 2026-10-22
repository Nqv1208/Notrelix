/**
 * Per-stream checkpoint state machine (backend decision Q2, gateway v1).
 *
 * `streamVersion` is a gap-free counter allocated by the server per
 * `streamKey` (+1 per event). Rules:
 *
 *   - the first event after (re)subscribe sets the checkpoint;
 *   - a later event with `streamVersion === last + 1` advances it;
 *   - any other version (gap, duplicate version, reset) means local
 *     incremental history is unreliable: the caller must refetch authoritative
 *     state over REST. The checkpoint is then rebased to the received version
 *     so the next `+1` event is accepted (FE-RT-025).
 *
 * Duplicate frames with the same `eventId` are filtered before this tracker.
 * `resourceVersion` is never consulted.
 */

export type StreamObservation =
  | { readonly kind: "first" }
  | { readonly kind: "next" }
  | {
      readonly kind: "recover";
      readonly reason: "gap" | "duplicate" | "reset";
      readonly expected: number;
      readonly received: number;
    };

export class StreamCheckpointTracker {
  private readonly checkpoints = new Map<string, number>();

  /** Records an observed version; the checkpoint always ends at `version`. */
  public observe(streamKey: string, version: number): StreamObservation {
    const last = this.checkpoints.get(streamKey);
    this.checkpoints.set(streamKey, version);

    if (last === undefined) return { kind: "first" };
    if (version === last + 1) return { kind: "next" };

    const expected = last + 1;
    const reason =
      version > expected ? "gap" : version === last ? "duplicate" : "reset";
    return { kind: "recover", reason, expected, received: version };
  }

  public get(streamKey: string): number | undefined {
    return this.checkpoints.get(streamKey);
  }

  /** Forgets every checkpoint whose key starts with `prefix`. */
  public resetByPrefix(prefix: string): void {
    for (const key of [...this.checkpoints.keys()]) {
      if (key.startsWith(prefix)) this.checkpoints.delete(key);
    }
  }

  public clear(): void {
    this.checkpoints.clear();
  }
}
