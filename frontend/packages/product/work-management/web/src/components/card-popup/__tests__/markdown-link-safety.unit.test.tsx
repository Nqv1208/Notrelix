import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { Markdown } from "../markdown";

// UC-COL-H04: the one renderer that draws links must never emit an executable
// or data URL as an href, and must escape markup. Rendered to static HTML, so
// the check needs no DOM.
function html(text: string, workspaceId?: string) {
  return renderToStaticMarkup(
    <Markdown text={text} workspaceId={workspaceId} />,
  );
}

describe("markdown links refuse unsafe schemes", () => {
  it.each([
    "[click](javascript:alert(1))",
    "[click](JavaScript:alert(1))",
    "[click](data:text/html;base64,PHNjcmlwdD4=)",
    "[click](vbscript:msgbox(1))",
    "[click](file:///etc/passwd)",
  ])("does not render %s as a link", (source) => {
    const out = html(source);
    expect(out).not.toContain("<a");
    expect(out).not.toMatch(/href/i);
    expect(out).toContain("click");
  });

  it("keeps https and mailto links as anchors with noopener", () => {
    const out = html(
      "[site](https://example.com/a) and [mail](mailto:ada@example.com)",
    );
    expect(out).toContain('href="https://example.com/a"');
    expect(out).toContain('href="mailto:ada@example.com"');
    expect(out).toContain('rel="noreferrer noopener"');
  });

  it("links a doc: page only inside a workspace", () => {
    expect(html("[page](doc:abc)", "ws-1")).toContain(
      'href="/workspaces/ws-1/docs/abc"',
    );
    expect(html("[page](doc:abc)")).not.toContain("<a");
  });
});

describe("markdown escapes markup", () => {
  it("shows a script tag as text", () => {
    const out = html("<script>alert(1)</script>");
    expect(out).not.toContain("<script");
    expect(out).toContain("&lt;script&gt;alert(1)&lt;/script&gt;");
  });

  it("shows an image with an error handler as text", () => {
    const out = html('<img src=x onerror="alert(1)">');
    expect(out).not.toContain("<img");
    expect(out).toContain("&lt;img");
  });

  it("does not turn an injected heading or code fence into an element with script content", () => {
    const out = html("# <svg onload=alert(1)>");
    expect(out).not.toContain("<svg");
    expect(out).toContain("&lt;svg");
  });
});
