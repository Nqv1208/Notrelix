import { cn } from "@notrelix/ui-web";
import { tokenizeBlocks, tokenizeInline } from "../../core/model/markup-model";

/**
 * Text with the inline markup a message may carry (UC-CHT-B08): bold, italic, code and http(s) links, drawn as elements from
 * tokens, never as parsed markup. A link opens in a new tab without the opener or the referrer.
 */
export function ChatInlineText({
  text,
  tone = "default",
}: {
  readonly text: string;
  readonly tone?: "own" | "default";
}) {
  const blocks = tokenizeBlocks(text);
  if (blocks.length === 1 && blocks[0]!.type === "paragraph") {
    return <Inline text={text} tone={tone} />;
  }
  // Block markup is drawn with block-level spans so it stays valid inside the message's paragraph.
  return (
    <>
      {blocks.map((block, index) => {
        if (block.type === "paragraph") {
          return (
            <span key={index} className="block">
              <Inline text={block.text} tone={tone} />
            </span>
          );
        }
        if (block.type === "quote") {
          return (
            <span
              key={index}
              className="block border-s-2 border-current/70 ps-2 opacity-80"
              data-block="quote"
            >
              <Inline text={block.text} tone={tone} />
            </span>
          );
        }
        return (
          <span
            key={index}
            role="list"
            className="block"
            data-block={block.ordered ? "ordered-list" : "list"}
          >
            {block.items.map((item, position) => (
              <span
                key={position}
                role="listitem"
                className="block ps-5 -indent-4"
              >
                <span aria-hidden="true">
                  {block.ordered ? `${position + 1}.` : "\u2022"}{" "}
                </span>
                <Inline text={item} tone={tone} />
              </span>
            ))}
          </span>
        );
      })}
    </>
  );
}

function Inline({
  text,
  tone,
}: {
  readonly text: string;
  readonly tone: "own" | "default";
}) {
  return (
    <>
      {tokenizeInline(text).map((token, index) => {
        switch (token.type) {
          case "text":
            return token.text;
          case "bold":
            return <strong key={index}>{token.text}</strong>;
          case "italic":
            return <em key={index}>{token.text}</em>;
          case "code":
            return (
              <code
                key={index}
                className={cn(
                  "rounded-[5px] px-1 font-mono",
                  tone === "own" ? "bg-primary-foreground/[.24]" : "bg-muted",
                )}
              >
                {token.text}
              </code>
            );
          case "link":
            return (
              <a
                key={index}
                href={token.href}
                target="_blank"
                rel="noopener noreferrer nofollow ugc"
                className="underline underline-offset-2 focus-visible:ring-2 focus-visible:ring-focus-ring focus-visible:outline-none"
              >
                {token.text}
              </a>
            );
        }
      })}
    </>
  );
}
