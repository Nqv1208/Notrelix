import { FileText } from "lucide-react";
import { cn } from "@notrelix/ui-web";

/** Blocks of a description: paragraphs, and "- " lines as a bulleted list. */
function descriptionBlocks(text: string) {
  return text
    .trim()
    .split(/\n{2,}/)
    .map((block) => {
      const lines = block.split("\n").map((line) => line.trimEnd());
      const items = lines.every((line) => /^\s*[-*]\s+/.test(line))
        ? lines.map((line) => line.replace(/^\s*[-*]\s+/, ""))
        : null;
      return items
        ? { kind: "list" as const, items }
        : { kind: "text" as const, text: block };
    });
}

const DOC_LINK =
  /\[([^\]]{1,200})\]\(doc:([\w-]{1,100})\)|\*\*([^*]{1,200})\*\*/g;

/**
 * Text with the `[title](doc:id)` links of the description drawn as document
 * chips. A chip links to the document in the item's workspace.
 */
function Inline({ text, workspaceId }: { text: string; workspaceId?: string }) {
  const parts: React.ReactNode[] = [];
  let last = 0;
  for (const match of text.matchAll(DOC_LINK)) {
    const index = match.index ?? 0;
    if (index > last) parts.push(text.slice(last, index));
    const [, label, docId, bold] = match;
    if (bold !== undefined) {
      parts.push(
        <b key={`b-${index}`} className="font-semibold">
          {bold}
        </b>,
      );
      last = index + match[0].length;
      continue;
    }
    parts.push(
      <a
        key={`${docId}-${index}`}
        data-doc-chip={docId}
        href={
          workspaceId ? `/workspaces/${workspaceId}/docs/${docId}` : undefined
        }
        className="inline-flex items-center gap-1 rounded-[5px] bg-brand-subtle px-1.5 font-medium text-brand-subtle-foreground no-underline hover:underline"
      >
        <FileText aria-hidden="true" className="size-3.5 shrink-0" />
        {label}
      </a>,
    );
    last = index + match[0].length;
  }
  if (last < text.length) parts.push(text.slice(last));
  return <>{parts}</>;
}

/** The item description: paragraphs and bulleted lists, as the page and the sheet show it. */
export function ItemDescriptionBody({
  text,
  className,
  workspaceId,
}: {
  text: string;
  className?: string;
  /** Workspace of the item, for the links of the documents it names. */
  workspaceId?: string;
}) {
  return (
    <div className={cn("max-w-[68ch] text-foreground", className)}>
      {descriptionBlocks(text).map((block, index) =>
        block.kind === "list" ? (
          <ul key={index} className="m-0 mb-1 list-disc pl-5">
            {block.items.map((item) => (
              <li key={item} className="my-[3px]">
                <Inline text={item} workspaceId={workspaceId} />
              </li>
            ))}
          </ul>
        ) : (
          <p
            key={index}
            className={cn(
              "m-0 whitespace-pre-line",
              // A line that is only bold is a heading of the list under it.
              /^\*\*[^*]+\*\*$/.test(block.text.trim()) ? "mb-1" : "mb-2.5",
            )}
          >
            <Inline text={block.text} workspaceId={workspaceId} />
          </p>
        ),
      )}
    </div>
  );
}
