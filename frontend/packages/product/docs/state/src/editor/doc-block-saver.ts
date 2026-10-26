import {
  diffBlockDrafts,
  isEmptyDiff,
  type BlockDraft,
  type BlockDraftPatch,
} from "@notrelix/docs-core";

/**
 * The writes the saver needs, as the producer offers them. The sequential
 * writer uses the per-block endpoints; a batch writer with `expectedVersion`
 * replaces it without touching the saver.
 */
export interface BlockWriter {
  /** Creates a block right after `afterId` (a stored id) and answers its id. */
  create(draft: BlockDraft, afterId: string | null): Promise<string>;
  update(id: string, patch: BlockDraftPatch, draft: BlockDraft): Promise<void>;
  remove(id: string): Promise<void>;
}

export type SaverStatus = "saved" | "saving" | "error";

export interface DocBlockSaverOptions {
  readonly debounceMs?: number;
  readonly onStatus?: (status: SaverStatus) => void;
  /** A block was stored under another id than the editor gave it. */
  readonly onIdAssigned?: (clientId: string, storedId: string) => void;
  /** The writes of one save went through; `drafts` is the stored document. */
  readonly onSaved?: (drafts: readonly BlockDraft[]) => void;
  /** The producer refused a write because the block changed (412). */
  readonly onConflict?: (blockId: string, error: unknown) => void;
  readonly schedule?: (run: () => void, ms: number) => unknown;
  readonly cancel?: (handle: unknown) => void;
}

function statusOf(error: unknown): number | null {
  if (error && typeof error === "object" && "status" in error) {
    const status = (error as { status?: unknown }).status;
    return typeof status === "number" ? status : null;
  }
  return null;
}

/**
 * Keeps the stored document equal to the editor's: it debounces edits, diffs
 * the top-level blocks against the last stored snapshot and writes the smallest
 * set of creates, updates, moves and deletes. The snapshot moves forward one
 * write at a time, so a failed save resumes where it stopped instead of
 * writing a block twice; typed text is never dropped, a failure only keeps the
 * latest document pending until a retry.
 */
export class DocBlockSaver {
  private snapshot: BlockDraft[] = [];
  private latest: BlockDraft[] | null = null;
  private timer: unknown = null;
  private running: Promise<void> | null = null;
  private again = false;
  private renamed = new Map<string, string>();
  private status: SaverStatus = "saved";
  private disposed = false;

  constructor(
    private readonly writer: BlockWriter,
    private readonly options: DocBlockSaverOptions = {},
  ) {}

  /** The stored document the next diff starts from (initial load, refetch). */
  reset(snapshot: readonly BlockDraft[]): void {
    this.snapshot = snapshot.map((draft) => ({ ...draft }));
    this.latest = null;
    this.renamed.clear();
    this.setStatus("saved");
  }

  /** The editor's document changed: save it after the debounce. */
  change(next: readonly BlockDraft[]): void {
    if (this.disposed) return;
    this.latest = next.map((draft) => ({
      ...draft,
      id: this.renamed.get(draft.id) ?? draft.id,
    }));
    if (this.hasWork()) this.setStatus("saving");
    this.cancelTimer();
    const schedule =
      this.options.schedule ?? ((run, ms) => setTimeout(run, ms));
    this.timer = schedule(() => {
      this.timer = null;
      void this.flush();
    }, this.options.debounceMs ?? 600);
  }

  /** Writes whatever is pending now (route leave, hidden tab, unmount). */
  flush(): Promise<void> {
    this.cancelTimer();
    if (this.running) {
      this.again = true;
      return this.running;
    }
    this.running = this.run().finally(() => {
      this.running = null;
    });
    return this.running;
  }

  retry(): Promise<void> {
    return this.flush();
  }

  dispose(): void {
    this.cancelTimer();
    this.disposed = true;
  }

  getStatus(): SaverStatus {
    return this.status;
  }

  private hasWork(): boolean {
    return (
      this.latest !== null &&
      !isEmptyDiff(diffBlockDrafts(this.snapshot, this.latest))
    );
  }

  private cancelTimer(): void {
    if (this.timer === null) return;
    (this.options.cancel ?? ((handle) => clearTimeout(handle as number)))(
      this.timer,
    );
    this.timer = null;
  }

  private setStatus(status: SaverStatus): void {
    if (this.status === status) return;
    this.status = status;
    this.options.onStatus?.(status);
  }

  private async run(): Promise<void> {
    do {
      this.again = false;
      const target = this.latest;
      if (!target) {
        this.setStatus("saved");
        return;
      }
      const diff = diffBlockDrafts(this.snapshot, target);
      if (isEmptyDiff(diff)) {
        this.setStatus("saved");
        continue;
      }
      this.setStatus("saving");
      try {
        await this.apply(diff, target);
      } catch (error) {
        this.setStatus("error");
        const status = statusOf(error);
        if (status === 412) {
          const blockId = this.lastBlockId ?? "";
          this.options.onConflict?.(blockId, error);
        }
        return;
      }
      this.options.onSaved?.(this.snapshot);
    } while (this.again || this.hasWork());
    this.setStatus("saved");
  }

  private lastBlockId: string | null = null;

  private async apply(
    diff: ReturnType<typeof diffBlockDrafts>,
    target: readonly BlockDraft[],
  ): Promise<void> {
    const draftOf = (id: string) => target.find((draft) => draft.id === id);
    for (const id of diff.deletes) {
      this.lastBlockId = id;
      await this.writer.remove(id);
      this.snapshot = this.snapshot.filter((draft) => draft.id !== id);
    }
    for (const update of diff.updates) {
      this.lastBlockId = update.id;
      await this.writer.update(update.id, update.patch, update.draft);
      this.snapshot = this.snapshot.map((draft) =>
        draft.id === update.id ? { ...update.draft } : draft,
      );
    }
    // Creates and moves walk the document in order, so the block each one
    // goes after is already in place.
    for (const draft of target) {
      const create = diff.creates.find((entry) => entry.draft.id === draft.id);
      const move = diff.moves.find((entry) => entry.id === draft.id);
      if (!create && !move) continue;
      const afterId = (create ?? move)?.afterId ?? null;
      this.lastBlockId = draft.id;
      const stored = move ? (draftOf(draft.id) ?? draft) : draft;
      // The producer has no move write: a moved block is written again after
      // its new neighbour and the old one removed.
      const clientId = draft.id;
      const id = await this.writer.create(stored, afterId);
      if (move) await this.writer.remove(clientId);
      this.snapshot = this.snapshot.filter((entry) => entry.id !== clientId);
      const at = afterId
        ? this.snapshot.findIndex((entry) => entry.id === afterId) + 1
        : 0;
      this.snapshot.splice(at, 0, { ...stored, id });
      if (id !== clientId) {
        this.renamed.set(clientId, id);
        for (const entry of target) {
          if (entry.id === clientId) entry.id = id;
        }
        if (this.latest) {
          this.latest = this.latest.map((entry) =>
            entry.id === clientId ? { ...entry, id } : entry,
          );
        }
        for (const entry of diff.creates) {
          if (entry.afterId === clientId) entry.afterId = id;
        }
        for (const entry of diff.moves) {
          if (entry.afterId === clientId) entry.afterId = id;
        }
        this.options.onIdAssigned?.(clientId, id);
      }
    }
  }
}
