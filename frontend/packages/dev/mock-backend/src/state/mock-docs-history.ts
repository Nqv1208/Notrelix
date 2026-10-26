/**
 * Documents history and templates of the mock backend: the entries a page's
 * history holds (each a copy of the blocks the page had) and the workspace's
 * page templates. Like the producer, an entry is never edited, a restore adds an
 * entry instead of rewriting the history, and a template is creation input.
 */

import {
  INVOICE_DOC_ID,
  INVOICE_HISTORY,
  invoiceSnapshot,
} from "./mock-docs-invoice";
import type {
  MockBlockRecord,
  MockPageTemplateRecord,
  MockPageVersionRecord,
} from "./records";
import type { MockStore } from "./mock-store";
import {
  readBlockBody,
  writeBlockBody,
} from "../contexts/documents/block-body";

const ATTACHMENT_TYPES = new Set(["image", "file", "video"]);

function copyBlocks(blocks: readonly MockBlockRecord[]): MockBlockRecord[] {
  return blocks.map((block) => ({ ...block }));
}

/** The text of the blocks, in tree order: what "has the content changed" compares. */
function fingerprint(
  title: string,
  blocks: readonly MockBlockRecord[],
): string {
  return JSON.stringify([
    title,
    [...blocks]
      .sort((a, b) => a.position - b.position || a.id.localeCompare(b.id))
      .map((block) => [
        block.id,
        block.parentBlockId ?? null,
        block.type,
        block.properties,
        block.position,
      ]),
  ]);
}

export type VersionRestoreResult =
  | { readonly ok: true; readonly version: MockPageVersionRecord }
  | { readonly ok: false; readonly currentBlocks: MockBlockRecord[] };

export class MockDocsHistoryState {
  private readonly versions = new Map<string, MockPageVersionRecord>();
  private readonly templates = new Map<string, MockPageTemplateRecord>();

  constructor(private readonly store: MockStore) {}

  // ─── History ──────────────────────────────────────────────────────────────

  /** The page's entries, newest first. */
  listVersions(pageId: string): MockPageVersionRecord[] {
    return Array.from(this.versions.values())
      .filter((entry) => entry.pageId === pageId)
      .sort((a, b) => b.versionNumber - a.versionNumber);
  }

  getVersion(pageId: string, versionId: string): MockPageVersionRecord | null {
    const entry = this.versions.get(versionId);
    return entry && entry.pageId === pageId ? entry : null;
  }

  /** Adds an entry holding the blocks given (or the page's blocks as they are now). */
  addVersion(
    pageId: string,
    input: {
      readonly userId: string | null;
      readonly kind: MockPageVersionRecord["kind"];
      readonly name?: string | null;
      readonly createdAt?: string;
      readonly blocks?: readonly MockBlockRecord[];
    },
  ): MockPageVersionRecord {
    const page = this.store.getPage(pageId);
    const last = this.listVersions(pageId)[0]?.versionNumber ?? 0;
    const entry: MockPageVersionRecord = {
      id: this.store.nextId("pversion"),
      pageId,
      versionNumber: last + 1,
      kind: input.kind,
      name: input.name?.trim() ? input.name.trim() : null,
      createdBy: input.userId,
      createdAt: input.createdAt ?? this.store.now(),
      title: page?.title ?? "",
      blocks: copyBlocks(input.blocks ?? this.store.getPageBlocks(pageId)),
    };
    this.versions.set(entry.id, entry);
    return entry;
  }

  /**
   * Puts the page's blocks back to an entry: only when `seen` names every live
   * block at its current version; otherwise nothing changes. What is replaced
   * stays recoverable (an entry is added first unless the newest already holds it).
   */
  restoreVersion(
    pageId: string,
    versionId: string,
    userId: string,
    seen: readonly { blockId: string; version: number }[],
  ): VersionRestoreResult | null {
    const target = this.getVersion(pageId, versionId);
    if (!target) return null;

    const live = this.store.getPageBlocks(pageId);
    const seenBy = new Map(seen.map((block) => [block.blockId, block.version]));
    const same =
      seenBy.size === live.length &&
      live.every((block) => seenBy.get(block.id) === block.version);
    if (!same) return { ok: false, currentBlocks: live };

    const page = this.store.getPage(pageId);
    const latest = this.listVersions(pageId)[0];
    if (
      !latest ||
      fingerprint(latest.title, latest.blocks) !==
        fingerprint(page?.title ?? "", live)
    ) {
      this.addVersion(pageId, {
        userId,
        kind: "automatic",
        name: `Before restoring version ${target.versionNumber}`,
      });
    }

    const before = new Map(live.map((block) => [block.id, block.version]));
    const restored = target.blocks.map((block): MockBlockRecord => ({
      ...block,
      version: (before.get(block.id) ?? 0) + 1,
      updatedAt: this.store.now(),
    }));
    this.store.restorePageBlocks(pageId, restored);
    const version = this.addVersion(pageId, {
      userId,
      kind: "restore",
      name: `Restored from version ${target.versionNumber}`,
    });
    return { ok: true, version };
  }

  // ─── Templates ────────────────────────────────────────────────────────────

  listTemplates(workspaceId: string): MockPageTemplateRecord[] {
    return Array.from(this.templates.values())
      .filter((template) => template.workspaceId === workspaceId)
      .sort((a, b) => a.name.localeCompare(b.name));
  }

  getTemplate(
    workspaceId: string,
    templateId: string,
  ): MockPageTemplateRecord | null {
    const template = this.templates.get(templateId);
    return template && template.workspaceId === workspaceId ? template : null;
  }

  createTemplate(
    pageId: string,
    input: { name: string; description?: string | null; userId: string },
  ): MockPageTemplateRecord | null {
    const page = this.store.getPage(pageId);
    if (!page) return null;
    const template: MockPageTemplateRecord = {
      id: this.store.nextId("ptemplate"),
      workspaceId: page.workspaceId,
      name: input.name.trim(),
      description: input.description?.trim() ? input.description.trim() : null,
      title: page.title,
      icon: page.icon ?? "",
      createdBy: input.userId,
      createdAt: this.store.now(),
      blocks: copyBlocks(this.store.getPageBlocks(pageId)),
    };
    this.templates.set(template.id, template);
    return template;
  }

  /**
   * A page with copies of the template's blocks under new ids. Images, files and
   * videos are not copied (an attachment belongs to the page it was added to);
   * the blocks under a dropped block go with it.
   */
  createPageFromTemplate(
    template: MockPageTemplateRecord,
    input: {
      title?: string | null;
      parentId?: string | null;
      spaceId?: string | null;
    },
  ): { pageId: string; droppedBlockCount: number } {
    const page = this.store.createPage(template.workspaceId, {
      title: input.title?.trim() ? input.title.trim() : template.title,
      icon: template.icon || undefined,
      parentId: input.parentId ?? undefined,
      spaceId: input.spaceId ?? null,
    });

    const dropped = new Set(
      template.blocks
        .filter((block) => ATTACHMENT_TYPES.has(block.type))
        .map((block) => block.id),
    );
    for (let grew = true; grew;) {
      grew = false;
      for (const block of template.blocks) {
        if (
          block.parentBlockId &&
          dropped.has(block.parentBlockId) &&
          !dropped.has(block.id)
        ) {
          dropped.add(block.id);
          grew = true;
        }
      }
    }

    const ids = new Map<string, string>();
    for (const block of template.blocks) {
      if (!dropped.has(block.id)) ids.set(block.id, this.store.nextId("block"));
    }
    for (const block of template.blocks) {
      const id = ids.get(block.id);
      if (!id) continue;
      this.store.createBlock(page.id, {
        id,
        type: block.type,
        properties: block.properties,
        position: block.position,
        parentBlockId: block.parentBlockId
          ? (ids.get(block.parentBlockId) ?? null)
          : null,
      });
    }
    return { pageId: page.id, droppedBlockCount: dropped.size };
  }

  // ─── Seed ─────────────────────────────────────────────────────────────────

  /**
   * The invoice document's history: one entry per seeded activity (oldest
   * first), each holding the blocks the page had then (`invoiceSnapshot`).
   */
  seedInvoiceHistory(): void {
    const blocks = this.store.getPageBlocks(INVOICE_DOC_ID);
    [...INVOICE_HISTORY].reverse().forEach((entry, reversedIndex, all) => {
      const index = all.length - 1 - reversedIndex;
      const shape = invoiceSnapshot(index);
      const captured = blocks.flatMap((block, position): MockBlockRecord[] => {
        if (shape.without.has(position)) return [];
        const override = shape.text.get(position);
        if (override === undefined) return [{ ...block }];
        return [
          {
            ...block,
            properties: writeBlockBody({
              content: { v: 1, runs: [{ text: override, marks: [] }] },
              properties: readBlockBody(block).properties,
            }),
          },
        ];
      });
      const restore = entry.detail.startsWith("Khôi phục");
      this.addVersion(INVOICE_DOC_ID, {
        userId: entry.userId,
        kind: entry.versionName ? "named" : restore ? "restore" : "automatic",
        name: entry.versionName ?? (restore ? entry.detail : null),
        createdAt: entry.at,
        blocks: captured,
      });
    });
  }

  clear(): void {
    this.versions.clear();
    this.templates.clear();
  }

  snapshot(): {
    pageVersions: MockPageVersionRecord[];
    pageTemplates: MockPageTemplateRecord[];
  } {
    return {
      pageVersions: Array.from(this.versions.values()),
      pageTemplates: Array.from(this.templates.values()),
    };
  }
}
