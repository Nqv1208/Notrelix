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

/** The item description: paragraphs and bulleted lists, as the page and the sheet show it. */
export function ItemDescriptionBody({
  text,
  className,
}: {
  text: string;
  className?: string;
}) {
  return (
    <div className={cn("max-w-[68ch] text-foreground", className)}>
      {descriptionBlocks(text).map((block, index) =>
        block.kind === "list" ? (
          <ul key={index} className="m-0 mb-1 list-disc pl-5">
            {block.items.map((item) => (
              <li key={item} className="my-[3px]">
                {item}
              </li>
            ))}
          </ul>
        ) : (
          <p key={index} className="m-0 mb-2.5 whitespace-pre-line">
            {block.text}
          </p>
        ),
      )}
    </div>
  );
}
