import { Fragment, type ReactNode } from "react";

/**
 * A small Markdown renderer for descriptions: it builds React elements and never
 * injects HTML, so text cannot become markup. It knows what the editor's toolbar
 * writes: headings, paragraphs, lists, task items, quotes, code, rules, tables,
 * bold, italic, strike, code and links (http, https, mailto and `doc:` pages).
 */

const INLINE =
  /(\*\*(.+?)\*\*)|(~~(.+?)~~)|(`([^`]+)`)|(\*([^*\s][^*]*?)\*)|(\[([^\]]+)\]\(([^)\s]+)\))/g;

function Inline({ text, workspaceId }: { text: string; workspaceId?: string }) {
  const out: ReactNode[] = [];
  let last = 0;
  for (const m of text.matchAll(INLINE)) {
    const at = m.index ?? 0;
    if (at > last) out.push(text.slice(last, at));
    const key = `i${at}`;
    if (m[2] !== undefined)
      out.push(
        <b key={key} className="font-semibold">
          <Inline text={m[2]} workspaceId={workspaceId} />
        </b>,
      );
    else if (m[4] !== undefined)
      out.push(
        <s key={key}>
          <Inline text={m[4]} workspaceId={workspaceId} />
        </s>,
      );
    else if (m[6] !== undefined)
      out.push(
        <code
          key={key}
          className="rounded bg-state-hover-strong px-1 font-mono text-[0.92em]"
        >
          {m[6]}
        </code>,
      );
    else if (m[8] !== undefined)
      out.push(
        <i key={key}>
          <Inline text={m[8]} workspaceId={workspaceId} />
        </i>,
      );
    else if (m[10] !== undefined && m[11] !== undefined) {
      const target = m[11];
      const href = target.startsWith("doc:")
        ? workspaceId
          ? `/workspaces/${workspaceId}/docs/${encodeURIComponent(target.slice(4))}`
          : undefined
        : /^(https?:|mailto:)/i.test(target)
          ? target
          : undefined;
      out.push(
        href ? (
          <a
            key={key}
            href={href}
            target={target.startsWith("doc:") ? undefined : "_blank"}
            rel="noreferrer noopener"
            className="text-brand-subtle-foreground underline"
          >
            {m[10]}
          </a>
        ) : (
          m[10]
        ),
      );
    }
    last = at + m[0].length;
  }
  if (last < text.length) out.push(text.slice(last));
  return <>{out}</>;
}

type Block =
  | { kind: "p"; text: string }
  | { kind: "h"; level: number; text: string }
  | { kind: "ul" | "ol"; items: string[] }
  | { kind: "quote"; text: string }
  | { kind: "code"; text: string }
  | { kind: "hr" }
  | { kind: "table"; rows: string[][] };

function parseBlocks(source: string): Block[] {
  const lines = source.replace(/\r\n?/g, "\n").split("\n");
  const blocks: Block[] = [];
  let i = 0;
  const cells = (line: string) =>
    line
      .trim()
      .replace(/^\||\|$/g, "")
      .split("|")
      .map((c) => c.trim());
  while (i < lines.length) {
    const line = lines[i] ?? "";
    if (line.trim() === "") {
      i += 1;
      continue;
    }
    if (line.trimStart().startsWith("```")) {
      const body: string[] = [];
      i += 1;
      while (
        i < lines.length &&
        !(lines[i] ?? "").trimStart().startsWith("```")
      ) {
        body.push(lines[i] ?? "");
        i += 1;
      }
      i += 1;
      blocks.push({ kind: "code", text: body.join("\n") });
      continue;
    }
    const heading = /^(#{1,3})\s+(.*)$/.exec(line);
    if (heading) {
      blocks.push({
        kind: "h",
        level: heading[1]!.length,
        text: heading[2] ?? "",
      });
      i += 1;
      continue;
    }
    if (/^\s*([-*_])\1{2,}\s*$/.test(line)) {
      blocks.push({ kind: "hr" });
      i += 1;
      continue;
    }
    if (/^\s*>/.test(line)) {
      const body: string[] = [];
      while (i < lines.length && /^\s*>/.test(lines[i] ?? "")) {
        body.push((lines[i] ?? "").replace(/^\s*>\s?/, ""));
        i += 1;
      }
      blocks.push({ kind: "quote", text: body.join("\n") });
      continue;
    }
    if (
      line.trimStart().startsWith("|") &&
      /^\s*\|?\s*:?-{2,}/.test(lines[i + 1] ?? "")
    ) {
      const rows = [cells(line)];
      i += 2;
      while (i < lines.length && (lines[i] ?? "").trimStart().startsWith("|")) {
        rows.push(cells(lines[i] ?? ""));
        i += 1;
      }
      blocks.push({ kind: "table", rows });
      continue;
    }
    const list = /^\s*([-*]|\d+\.)\s+/.exec(line);
    if (list) {
      const ordered = /\d/.test(list[1] ?? "");
      const items: string[] = [];
      while (i < lines.length) {
        const m = /^\s*([-*]|\d+\.)\s+(.*)$/.exec(lines[i] ?? "");
        if (!m || /\d/.test(m[1] ?? "") !== ordered) break;
        items.push(m[2] ?? "");
        i += 1;
      }
      blocks.push({ kind: ordered ? "ol" : "ul", items });
      continue;
    }
    const body: string[] = [];
    while (
      i < lines.length &&
      (lines[i] ?? "").trim() !== "" &&
      !/^(#{1,3}\s|\s*>|\s*([-*]|\d+\.)\s|```)/.test(lines[i] ?? "")
    ) {
      body.push(lines[i] ?? "");
      i += 1;
    }
    blocks.push({ kind: "p", text: body.join("\n") });
  }
  return blocks;
}

const HEADING = ["", "text-[20px]", "text-[17px]", "text-[15px]"];

/** The description as the card shows it. */
export function Markdown({
  text,
  workspaceId,
}: {
  text: string;
  workspaceId?: string;
}) {
  return (
    <>
      {parseBlocks(text).map((b, n) => {
        switch (b.kind) {
          case "h":
            return (
              <p
                key={n}
                className={`m-0 mb-1.5 font-[650] ${HEADING[b.level]}`}
              >
                <Inline text={b.text} workspaceId={workspaceId} />
              </p>
            );
          case "ul":
          case "ol": {
            const List = b.kind;
            return (
              <List
                key={n}
                className={`m-0 mb-1.5 pl-5 ${b.kind === "ul" ? "list-disc" : "list-decimal"}`}
              >
                {b.items.map((item, k) => {
                  const task = /^\[( |x|X)\]\s+(.*)$/.exec(item);
                  return (
                    <li key={k} className="my-0.5">
                      {task ? (
                        <>
                          {task[1] === " " ? "☐ " : "☑ "}
                          <Inline
                            text={task[2] ?? ""}
                            workspaceId={workspaceId}
                          />
                        </>
                      ) : (
                        <Inline text={item} workspaceId={workspaceId} />
                      )}
                    </li>
                  );
                })}
              </List>
            );
          }
          case "quote":
            return (
              <blockquote
                key={n}
                className="m-0 mb-1.5 border-l-[3px] border-line-strong pl-2 text-muted-foreground"
              >
                <Inline text={b.text} workspaceId={workspaceId} />
              </blockquote>
            );
          case "code":
            return (
              <pre
                key={n}
                className="m-0 mb-1.5 overflow-x-auto rounded-md bg-state-hover-strong p-2 font-mono text-[12.5px]"
              >
                {b.text}
              </pre>
            );
          case "hr":
            return (
              <hr key={n} className="my-2 border-0 border-t border-line" />
            );
          case "table":
            return (
              <table key={n} className="mb-1.5 border-collapse text-[13.5px]">
                <tbody>
                  {b.rows.map((row, r) => (
                    <tr key={r}>
                      {row.map((cell, c) => {
                        const Cell = r === 0 ? "th" : "td";
                        return (
                          <Cell
                            key={c}
                            className="border border-line px-2 py-1 text-left font-normal first:font-normal"
                          >
                            {r === 0 ? (
                              <b>{cell}</b>
                            ) : (
                              <Inline text={cell} workspaceId={workspaceId} />
                            )}
                          </Cell>
                        );
                      })}
                    </tr>
                  ))}
                </tbody>
              </table>
            );
          default:
            return (
              <p key={n} className="m-0 mb-1.5 whitespace-pre-line">
                <Inline text={b.text} workspaceId={workspaceId} />
              </p>
            );
        }
      })}
      <Fragment />
    </>
  );
}
