import React, { type ReactNode } from "react";
import { View, Text, StyleSheet } from "react-native";
import {
  plainTextOfRuns,
  type Block,
  type Run,
  type TableData,
} from "@notrelix/docs-core";
import { buildDocTree, type DocNode } from "../render/doc-tree";
import { DEFAULT_DOC_LABELS, type MobileDocLabels } from "../render/doc-labels";
import {
  CALLOUT_COLORS,
  DOC_COLORS,
  HIGHLIGHT_COLORS,
} from "../render/doc-theme";
import { MobileRichText } from "./mobile-rich-text";

export interface MobileDocRendererProps {
  readonly blocks: readonly Block[];
  readonly labels?: Partial<MobileDocLabels>;
  readonly onOpenLink?: (href: string) => void;
  readonly onOpenPage?: (pageId: string) => void;
  readonly onOpenMember?: (userId: string) => void;
  /**
   * Draws an image or file block. The package has no attachment loader; the
   * host supplies one (it knows the session). Without it the block shows a
   * placeholder with its alt text and caption.
   */
  readonly renderAttachment?: (block: Block) => ReactNode;
}

interface Env {
  readonly labels: MobileDocLabels;
  readonly links: Pick<
    MobileDocRendererProps,
    "onOpenLink" | "onOpenPage" | "onOpenMember"
  >;
  readonly renderAttachment: MobileDocRendererProps["renderAttachment"];
}

/** A page's blocks drawn read-only with native views only. */
export function MobileDocRenderer({
  blocks,
  labels,
  onOpenLink,
  onOpenPage,
  onOpenMember,
  renderAttachment,
}: MobileDocRendererProps): React.ReactNode {
  const env: Env = {
    labels: { ...DEFAULT_DOC_LABELS, ...labels },
    links: { onOpenLink, onOpenPage, onOpenMember },
    renderAttachment,
  };
  return (
    <View style={styles.page}>{renderNodes(buildDocTree(blocks), env)}</View>
  );
}

function renderNodes(nodes: readonly DocNode[], env: Env): ReactNode[] {
  return nodes.map((node) => (
    <BlockView key={node.block.id} node={node} env={env} />
  ));
}

function Runs({
  runs,
  env,
  style,
}: {
  runs: readonly Run[];
  env: Env;
  style?: object;
}): React.ReactNode {
  return <MobileRichText runs={runs} style={style} {...env.links} />;
}

function Nested({ node, env }: { node: DocNode; env: Env }): React.ReactNode {
  return node.children.length ? (
    <View style={styles.nested}>{renderNodes(node.children, env)}</View>
  ) : null;
}

function BlockView({
  node,
  env,
}: {
  node: DocNode;
  env: Env;
}): React.ReactNode {
  const { block, runs } = node;
  switch (block.type) {
    case "heading_1":
    case "heading_2":
    case "heading_3": {
      const level =
        block.type === "heading_1" ? 1 : block.type === "heading_2" ? 2 : 3;
      return (
        <View style={styles.block} accessibilityRole="header">
          <Runs runs={runs} env={env} style={HEADINGS[level]} />
          <Nested node={node} env={env} />
        </View>
      );
    }
    case "bulleted_list":
    case "numbered_list":
      return (
        <View style={styles.block}>
          <View style={styles.row}>
            <Text style={styles.marker}>
              {block.type === "numbered_list" ? `${node.ordinal ?? 1}.` : "•"}
            </Text>
            <View style={styles.fill}>
              <Runs runs={runs} env={env} />
            </View>
          </View>
          <Nested node={node} env={env} />
        </View>
      );
    case "todo": {
      const checked = block.properties.checked === true;
      return (
        <View style={styles.block}>
          <View
            style={styles.row}
            accessible
            accessibilityRole="checkbox"
            accessibilityState={{ checked }}
            accessibilityLabel={checked ? env.labels.done : env.labels.notDone}
          >
            <Text style={styles.marker}>{checked ? "☑" : "☐"}</Text>
            <View style={styles.fill}>
              <Runs
                runs={runs}
                env={env}
                style={checked ? styles.done : undefined}
              />
            </View>
          </View>
          <Nested node={node} env={env} />
        </View>
      );
    }
    case "quote":
      return (
        <View style={[styles.block, styles.quote]}>
          <Runs runs={runs} env={env} style={styles.quoteText} />
          <Nested node={node} env={env} />
        </View>
      );
    case "callout": {
      const tone = block.properties.tone ?? "neutral";
      return (
        <View
          style={[
            styles.block,
            styles.callout,
            { backgroundColor: CALLOUT_COLORS[tone] },
          ]}
        >
          {block.properties.icon ? (
            <Text style={styles.marker}>{block.properties.icon}</Text>
          ) : null}
          <View style={styles.fill}>
            <Runs runs={runs} env={env} />
            <Nested node={node} env={env} />
          </View>
        </View>
      );
    }
    case "code":
      return (
        <View style={[styles.block, styles.codeBlock]}>
          <Text style={styles.codeText}>{plainTextOfRuns(runs)}</Text>
        </View>
      );
    case "divider":
      return <View style={styles.divider} />;
    case "toggle":
      // Read-only: the content is always shown, never hidden behind a tap.
      return (
        <View style={styles.block}>
          <View style={styles.row}>
            <Text style={styles.marker}>▾</Text>
            <View style={styles.fill}>
              <Runs runs={runs} env={env} />
            </View>
          </View>
          <Nested node={node} env={env} />
        </View>
      );
    case "table":
      return block.properties.table ? (
        <TableView table={block.properties.table} env={env} />
      ) : null;
    case "image":
    case "file":
      return <AttachmentView node={node} env={env} />;
    case "page_reference":
      return (
        <View style={[styles.block, styles.reference]}>
          <Text
            style={styles.referenceText}
            accessibilityRole="button"
            onPress={() =>
              block.properties.linkedPageId
                ? env.links.onOpenPage?.(block.properties.linkedPageId)
                : undefined
            }
          >
            {env.labels.page}
          </Text>
        </View>
      );
    case "board_reference":
      return (
        <View style={[styles.block, styles.reference]}>
          <Text style={styles.referenceText}>
            {block.properties.title || env.labels.board}
          </Text>
        </View>
      );
    case "paragraph":
    default:
      return runs.length || node.children.length ? (
        <View style={styles.block}>
          <Runs runs={runs} env={env} />
          <Nested node={node} env={env} />
        </View>
      ) : (
        <View style={styles.spacer} />
      );
  }
}

function TableView({
  table,
  env,
}: {
  table: TableData;
  env: Env;
}): React.ReactNode {
  return (
    <View style={[styles.block, styles.table]}>
      {table.rows.map((row, rowIndex) => (
        <View key={rowIndex} style={styles.tableRow}>
          {row.map((cell, cellIndex) => (
            <View
              key={cellIndex}
              style={[
                styles.tableCell,
                cell.bg && cell.bg !== "none"
                  ? { backgroundColor: HIGHLIGHT_COLORS[cell.bg] }
                  : null,
              ]}
            >
              <Runs
                runs={cell.runs}
                env={env}
                style={
                  table.headerRow && rowIndex === 0
                    ? styles.tableHead
                    : styles.tableText
                }
              />
            </View>
          ))}
        </View>
      ))}
    </View>
  );
}

function AttachmentView({
  node,
  env,
}: {
  node: DocNode;
  env: Env;
}): React.ReactNode {
  const { block } = node;
  const custom = env.renderAttachment?.(block);
  if (custom) return <View style={styles.block}>{custom}</View>;
  const isImage = block.type === "image";
  const caption = block.properties.captionRuns ?? [];
  const description = block.properties.alt || block.properties.caption || "";
  return (
    <View
      style={[styles.block, styles.attachment]}
      accessible
      accessibilityLabel={
        description || (isImage ? env.labels.image : env.labels.file)
      }
    >
      <Text style={styles.referenceText}>
        {isImage ? env.labels.image : env.labels.file}
      </Text>
      {caption.length ? (
        <Runs runs={caption} env={env} style={styles.caption} />
      ) : description ? (
        <Text style={styles.caption}>{description}</Text>
      ) : null}
    </View>
  );
}

const HEADINGS: Record<number, object> = {
  1: { fontSize: 28, lineHeight: 36, fontWeight: "700" },
  2: { fontSize: 22, lineHeight: 30, fontWeight: "700" },
  3: { fontSize: 18, lineHeight: 26, fontWeight: "600" },
};

const styles = StyleSheet.create({
  page: { paddingVertical: 8 },
  block: { marginVertical: 4 },
  spacer: { height: 12 },
  row: { flexDirection: "row", alignItems: "flex-start" },
  fill: { flex: 1 },
  nested: { marginLeft: 20 },
  marker: { width: 24, fontSize: 16, lineHeight: 24, color: DOC_COLORS.muted },
  done: { color: DOC_COLORS.muted, textDecorationLine: "line-through" },
  quote: {
    borderLeftWidth: 3,
    borderLeftColor: DOC_COLORS.line,
    paddingLeft: 12,
  },
  quoteText: { color: DOC_COLORS.muted, fontStyle: "italic" },
  callout: { flexDirection: "row", padding: 12, borderRadius: 8 },
  codeBlock: {
    padding: 12,
    borderRadius: 8,
    backgroundColor: DOC_COLORS.codeBackground,
  },
  codeText: {
    fontFamily: "Courier",
    fontSize: 14,
    lineHeight: 20,
    color: DOC_COLORS.text,
  },
  divider: {
    height: 1,
    marginVertical: 12,
    backgroundColor: DOC_COLORS.line,
  },
  table: { borderWidth: 1, borderColor: DOC_COLORS.line },
  tableRow: { flexDirection: "row" },
  tableCell: {
    flex: 1,
    padding: 8,
    borderWidth: 0.5,
    borderColor: DOC_COLORS.line,
  },
  tableHead: { fontSize: 14, lineHeight: 20, fontWeight: "700" },
  tableText: { fontSize: 14, lineHeight: 20 },
  reference: {
    padding: 12,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: DOC_COLORS.line,
  },
  referenceText: { fontSize: 14, color: DOC_COLORS.link },
  attachment: {
    padding: 12,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: DOC_COLORS.line,
    backgroundColor: DOC_COLORS.codeBackground,
  },
  caption: {
    marginTop: 4,
    fontSize: 13,
    lineHeight: 18,
    color: DOC_COLORS.muted,
  },
});
