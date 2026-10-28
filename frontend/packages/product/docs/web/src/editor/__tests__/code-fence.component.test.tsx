import { beforeAll, describe, expect, it } from "vitest";
import { FENCE_ENTER, FENCE_SPACE } from "../code-block";
import {
  headlessEditor,
  paragraph,
  pressEnter,
  stubLayout,
  typeText,
} from "./headless-editor";

beforeAll(stubLayout);

function fresh() {
  const editor = headlessEditor([paragraph("a")]);
  editor.commands.focus("end");
  return editor;
}

describe("code fence", () => {
  it("opens a plain code block on three backticks and a space", () => {
    const editor = fresh();
    typeText(editor, "``` ");
    expect(editor.getJSON().content?.[0]?.type).toBe("codeBlock");
    expect(editor.getJSON().content?.[0]?.attrs?.language ?? null).toBeNull();
  });

  it("does not take a typed word as the language", () => {
    const editor = fresh();
    typeText(editor, "```const ");
    const first = editor.getJSON().content?.[0];
    expect(first?.type).toBe("paragraph");
    expect((first?.content?.[0] as { text?: string } | undefined)?.text).toBe(
      "```const ",
    );
  });

  it("opens a block with the language on Enter after the fence and the name", () => {
    const editor = fresh();
    typeText(editor, "```ts");
    expect(pressEnter(editor)).toBe(true);
    const first = editor.getJSON().content?.[0];
    expect(first?.type).toBe("codeBlock");
    expect(first?.attrs?.language).toBe("ts");
    expect(first?.content).toBeUndefined();
  });

  it("opens a plain block on Enter after the bare fence", () => {
    const editor = fresh();
    typeText(editor, "```");
    pressEnter(editor);
    expect(editor.getJSON().content?.[0]?.type).toBe("codeBlock");
  });

  it("matches only whole fences", () => {
    expect(FENCE_SPACE.test("``` ")).toBe(true);
    expect(FENCE_SPACE.test("```a ")).toBe(false);
    expect(FENCE_ENTER.exec("```js")?.[1]).toBe("js");
    expect(FENCE_ENTER.test("``` js")).toBe(false);
  });
});
