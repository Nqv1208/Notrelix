import { describe, expect, it } from "vitest";
import { render, screen } from "@notrelix/testing";
import { ChatInlineText } from "../web/components/chat-inline-text";

describe("ChatInlineText (UC-CHT-B08)", () => {
  it("draws markup as elements and a link with safe attributes", () => {
    const { container } = render(
      <p>
        <ChatInlineText text="**hi** _you_ `x` https://x.test/a." />
      </p>,
    );
    expect(container.querySelector("strong")?.textContent).toBe("hi");
    expect(container.querySelector("em")?.textContent).toBe("you");
    expect(container.querySelector("code")?.textContent).toBe("x");
    const link = screen.getByRole("link", { name: "https://x.test/a" });
    expect(link.getAttribute("href")).toBe("https://x.test/a");
    expect(link.getAttribute("target")).toBe("_blank");
    expect(link.getAttribute("rel")).toBe("noopener noreferrer nofollow ugc");
    expect(container.textContent?.endsWith("https://x.test/a.")).toBe(true);
  });

  it("shows script, event attributes and a javascript address as inert text", () => {
    const hostile =
      '<script>window.__pwned=1</script><img src=x onerror="window.__pwned=1"> javascript:window.__pwned=1 [x](javascript:alert(1))';
    const { container } = render(
      <p>
        <ChatInlineText text={hostile} />
      </p>,
    );
    expect(container.textContent).toBe(hostile);
    expect(container.querySelector("script, img, a")).toBeNull();
    expect((window as unknown as { __pwned?: number }).__pwned).toBeUndefined();
  });

  it("draws quotes and lists as block elements and keeps hostile text inside them inert", () => {
    const { container } = render(
      <p>
        <ChatInlineText
          text={
            "> **said** <b>x</b>\n- one\n- <img src=x onerror=alert(1)>\n1. first"
          }
        />
      </p>,
    );
    expect(
      container.querySelector('[data-block="quote"] strong')?.textContent,
    ).toBe("said");
    expect(screen.getAllByRole("listitem")).toHaveLength(3);
    expect(container.querySelector("img, b")).toBeNull();
    expect(container.textContent).toContain("<img src=x onerror=alert(1)>");
  });

  it("gives every link rel noopener noreferrer and shows a data address as text", () => {
    const { container } = render(
      <p>
        <ChatInlineText text="https://a.test data:text/html,x mailto:a@b.test [y](javascript:alert(1))" />
      </p>,
    );
    const links = container.querySelectorAll("a");
    expect(links).toHaveLength(1);
    expect(links[0]!.getAttribute("rel")).toContain("noopener");
    expect(links[0]!.getAttribute("rel")).toContain("noreferrer");
    expect(container.textContent).toContain("data:text/html,x mailto:a@b.test");
  });
});
