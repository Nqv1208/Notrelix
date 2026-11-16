import { cn } from "@notrelix/ui-web";
import { tokenizeInline } from "../../core/model/markup-model";

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
