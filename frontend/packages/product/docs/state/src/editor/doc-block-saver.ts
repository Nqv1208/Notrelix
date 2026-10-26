import {
  diffBlockDrafts,
  draftEquals,
  isEmptyDiff,
  type BlockDraft,
  type BlockDraftPatch,
} from "@notrelix/docs-core";

/** One write of a save, as the producer's batch takes it. */
export interface BlockWrite {
  readonly op: "upsert" | "delete";
  readonly id: string;
  /** An upsert: the block as it is to be stored (a create sends all of it). */
  readonly draft?: BlockDraft;
  /** An edit sends only what changed; absent on a create. */
  readonly patch?: BlockDraftPatch;
  /** The version the caller read; required for an edit and a delete. */
  readonly expectedVersion?: number;
  /** The top-level block it goes right after; absent leaves an edit where it is. */
  readonly afterId?: string;
}

export interface SavedBlock {
  readonly id: string;
  readonly version: number;
}

/**
 * The writes the saver needs: the producer applies the operations in order in
 * one transaction (all or nothing). A stale `expectedVersion` rejects the whole
 * write with a `BlockVersionConflict` carrying the block as it is stored now.
 */
export interface BlockWriter {
  write(ops: readonly BlockWrite[]): Promise<readonly SavedBlock[]>;
}

/** The producer refused a write because a block changed meanwhile (412). */
export class BlockVersionConflict extends Error {
  constructor(readonly server: BlockDraft) {
    super("The block was changed elsewhere.");
    this.name = "BlockVersionConflict";
  }
}

export type SaverStatus = "saved" | "saving" | "error";

export interface BlockConflict {
  readonly blockId: string;
  /** What the member had in the block (null when they had removed it). */
  readonly local: BlockDraft | null;
  /** The block as stored now; the editor shows this one. */
  readonly server: BlockDraft;
}

export interface DocBlockSaverOptions {
  readonly debounceMs?: number;
  readonly onStatus?: (status: SaverStatus) => void;
  /** The writes of one save went through; `drafts` is the stored document. */
  readonly onSaved?: (drafts: readonly BlockDraft[]) => void;
  /** A block was refused as stale: the saver took the stored one instead. */
  readonly onConflict?: (conflict: BlockConflict) => void;
  /**
   * Blocks of the document changed outside the member's edits (a stored block
   * replaced a stale one, or others' changes were merged): the editor shows
   * `drafts`.
   */
  readonly onRebased?: (drafts: readonly BlockDraft[]) => void;
  readonly schedule?: (run: () => void, ms: number) => unknown;
  readonly cancel?: (handle: unknown) => void;
}

/** The producer takes at most this many operations per batch. */
export const MAX_BATCH_OPERATIONS = 500;
const MAX_CONFLICTS_PER_SAVE = 5;

/**
 * Turns the difference between the stored snapshot and the editor's blocks
 * into the batch's operations: deletes first, then every new or moved block
 * placed after its predecessor, in document order. The producer cannot place a
 * block first, so a block that has to be first is stored last and the blocks
 * behind it are placed after their predecessor in turn.
 */
export function planBlockWrites(
  snapshot: readonly BlockDraft[],
  target: readonly BlockDraft[],
): BlockWrite[] {
  const diff = diffBlockDrafts(snapshot, target);
  if (isEmptyDiff(diff)) return [];
  const stored = new Map(snapshot.map((draft) => [draft.id, draft]));
  const updates = new Map(diff.updates.map((entry) => [entry.id, entry.patch]));
  const creates = new Set(diff.creates.map((entry) => entry.draft.id));
  const moves = new Set(diff.moves.map((entry) => entry.id));

  const ops: BlockWrite[] = [];
  for (const id of diff.deletes) {
    ops.push({
      op: "delete",
      id,
      expectedVersion: stored.get(id)?.version,
    });
  }
  let rechain = false;
  target.forEach((draft, index) => {
    const previous = index > 0 ? (target[index - 1] as BlockDraft).id : null;
    const isCreate = creates.has(draft.id);
    const isMove = moves.has(draft.id);
    if (index === 0 && (isCreate || isMove)) rechain = true;
    const place = (isCreate || isMove || rechain) && previous !== null;
    const afterId = place ? (previous as string) : undefined;
    if (isCreate) {
      ops.push({
        op: "upsert",
        id: draft.id,
        draft,
        ...(afterId ? { afterId } : {}),
      });
      return;
    }
    const patch = updates.get(draft.id);
    if (!patch && !afterId) return;
    ops.push({
      op: "upsert",
      id: draft.id,
      draft,
      patch: patch ?? {},
      expectedVersion: stored.get(draft.id)?.version,
      ...(afterId ? { afterId } : {}),
    });
  });
  return ops;
}

/** The stored document after `ops` applied, as the producer applies them. */
export function applyBlockWrites(
  snapshot: readonly BlockDraft[],
  ops: readonly BlockWrite[],
  versions: ReadonlyMap<string, number>,
): BlockDraft[] {
  const next = snapshot.map((draft) => ({ ...draft }));
  for (const op of ops) {
    const at = next.findIndex((draft) => draft.id === op.id);
    if (op.op === "delete") {
      if (at >= 0) next.splice(at, 1);
      continue;
    }
    const draft = op.draft as BlockDraft;
    const version = versions.get(op.id) ?? draft.version;
    const written: BlockDraft = {
      ...draft,
      ...(version !== undefined ? { version } : {}),
    };
    if (at >= 0) next.splice(at, 1);
    const after = op.afterId
      ? next.findIndex((entry) => entry.id === op.afterId) + 1
      : at >= 0
        ? at
        : next.length;
    next.splice(after, 0, written);
  }
  return next;
}

/**
 * Keeps the stored document equal to the editor's: it debounces edits, diffs
 * the top-level blocks against the last stored snapshot and writes the smallest
 * batch of creates, edits, moves and deletes. The snapshot moves forward one
 * batch at a time, so a failed save resumes where it stopped instead of
 * writing a block twice; typed text is never dropped, a failure only keeps the
 * latest document pending until a retry. A block the producer refuses as stale
 * is replaced by the stored one (the member's text goes to `onConflict`) and
 * the rest of the save is retried.
 */
export class DocBlockSaver {
  private snapshot: BlockDraft[] = [];
  private latest: BlockDraft[] | null = null;
  private timer: unknown = null;
  private running: Promise<void> | null = null;
  private again = false;
  private status: SaverStatus = "saved";
  private disposed = false;

  constructor(
    private readonly writer: BlockWriter,
    private readonly options: DocBlockSaverOptions = {},
  ) {}

  /** The stored document the next diff starts from (initial load). */
  reset(snapshot: readonly BlockDraft[]): void {
    this.snapshot = snapshot.map((draft) => ({ ...draft }));
    this.latest = null;
    this.setStatus("saved");
  }

  /** The editor's document changed: save it after the debounce. */
  change(next: readonly BlockDraft[]): void {
    if (this.disposed) return;
    this.latest = next.map((draft) => ({ ...draft }));
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

  /** True while the member has changes the producer has not stored. */
  hasPending(): boolean {
    return this.hasWork();
  }

  /**
   * The stored document changed elsewhere (a refetch). Blocks the member is
   * not editing take the stored version; a block with pending edits keeps
   * them (a stale write is refused and resolved through `onConflict`).
   * Structure changes wait until nothing is pending. Answers whether
   * everything was merged.
   */
  mergeStored(stored: readonly BlockDraft[]): boolean {
    if (this.disposed) return true;
    const pendingIds = this.pendingIds();
    const document = this.latest ?? this.snapshot;
    const same =
      stored.length === document.length &&
      stored.every((draft, index) => {
        const local = document[index];
        return local && draftEquals(local, draft);
      });
    if (pendingIds.size === 0) {
      this.snapshot = stored.map((draft) => ({ ...draft }));
      if (this.latest) this.latest = stored.map((draft) => ({ ...draft }));
      if (!same) this.options.onRebased?.(stored);
      return true;
    }
    // Pending edits: only blocks both sides have and the member left alone.
    const byId = new Map(stored.map((draft) => [draft.id, draft]));
    let changed = false;
    const take = (list: BlockDraft[]) =>
      list.map((draft) => {
        const server = byId.get(draft.id);
        if (!server || pendingIds.has(draft.id)) return draft;
        if (!draftEquals(draft, server) || draft.version !== server.version) {
          changed = true;
          return { ...server };
        }
        return draft;
      });
    this.snapshot = take(this.snapshot);
    if (this.latest) this.latest = take(this.latest);
    if (changed && this.latest) this.options.onRebased?.(this.latest);
    return false;
  }

  private pendingIds(): Set<string> {
    if (!this.latest) return new Set();
    const diff = diffBlockDrafts(this.snapshot, this.latest);
    return new Set([
      ...diff.deletes,
      ...diff.updates.map((entry) => entry.id),
      ...diff.moves.map((entry) => entry.id),
      ...diff.creates.map((entry) => entry.draft.id),
    ]);
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
    let conflicts = 0;
    do {
      this.again = false;
      const target = this.latest;
      if (!target) {
        this.setStatus("saved");
        return;
      }
      const ops = planBlockWrites(this.snapshot, target);
      if (ops.length === 0) {
        this.setStatus("saved");
        continue;
      }
      this.setStatus("saving");
      try {
        await this.send(ops);
      } catch (error) {
        if (
          error instanceof BlockVersionConflict &&
          conflicts < MAX_CONFLICTS_PER_SAVE
        ) {
          conflicts += 1;
          this.takeStored(error.server);
          this.again = true;
          continue;
        }
        this.setStatus("error");
        return;
      }
      this.options.onSaved?.(this.snapshot);
    } while (this.again || this.hasWork());
    this.setStatus("saved");
  }

  private async send(ops: readonly BlockWrite[]): Promise<void> {
    for (let from = 0; from < ops.length; from += MAX_BATCH_OPERATIONS) {
      const chunk = ops.slice(from, from + MAX_BATCH_OPERATIONS);
      const saved = await this.writer.write(chunk);
      this.snapshot = applyBlockWrites(
        this.snapshot,
        chunk,
        new Map(saved.map((block) => [block.id, block.version])),
      );
    }
  }

  /** A stale block: the stored one replaces it in the snapshot and the editor. */
  private takeStored(server: BlockDraft): void {
    const local = this.latest?.find((draft) => draft.id === server.id) ?? null;
    const at = this.snapshot.findIndex((draft) => draft.id === server.id);
    if (at >= 0) this.snapshot[at] = { ...server };
    else this.snapshot.push({ ...server });
    if (this.latest) {
      const index = this.latest.findIndex((draft) => draft.id === server.id);
      if (index >= 0) this.latest[index] = { ...server };
      else {
        const previous = at > 0 ? this.snapshot[at - 1]?.id : undefined;
        const after = previous
          ? this.latest.findIndex((draft) => draft.id === previous) + 1
          : 0;
        this.latest.splice(after, 0, { ...server });
      }
    }
    this.options.onConflict?.({ blockId: server.id, local, server });
    this.options.onRebased?.(this.latest ?? this.snapshot);
  }
}
