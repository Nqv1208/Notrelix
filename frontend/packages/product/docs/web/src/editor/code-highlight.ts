/**
 * Syntax colours of a code block. The highlighter (lowlight) is a lazy chunk:
 * it loads the first time a page holds a code block, and a block with no
 * (or an unknown) language stays plain.
 */

/** The languages the picker offers (lowlight's common set, by its names). */
export const CODE_LANGUAGES: readonly { value: string; label: string }[] = [
  { value: "bash", label: "Bash" },
  { value: "c", label: "C" },
  { value: "cpp", label: "C++" },
  { value: "csharp", label: "C#" },
  { value: "css", label: "CSS" },
  { value: "diff", label: "Diff" },
  { value: "go", label: "Go" },
  { value: "graphql", label: "GraphQL" },
  { value: "xml", label: "HTML / XML" },
  { value: "java", label: "Java" },
  { value: "javascript", label: "JavaScript" },
  { value: "json", label: "JSON" },
  { value: "kotlin", label: "Kotlin" },
  { value: "markdown", label: "Markdown" },
  { value: "php", label: "PHP" },
  { value: "python", label: "Python" },
  { value: "ruby", label: "Ruby" },
  { value: "rust", label: "Rust" },
  { value: "scss", label: "SCSS" },
  { value: "sql", label: "SQL" },
  { value: "swift", label: "Swift" },
  { value: "typescript", label: "TypeScript" },
  { value: "yaml", label: "YAML" },
];

/** App-token classes for highlight.js's token names (light and dark follow the tokens). */
const TOKEN_CLASS: Readonly<Record<string, string>> = {
  "hljs-keyword": "text-brand-subtle-foreground",
  "hljs-built_in": "text-brand-subtle-foreground",
  "hljs-type": "text-brand-subtle-foreground",
  "hljs-selector-tag": "text-brand-subtle-foreground",
  "hljs-string": "text-success-text",
  "hljs-regexp": "text-success-text",
  "hljs-addition": "text-success-text",
  "hljs-template-variable": "text-success-text",
  "hljs-number": "text-warning-text",
  "hljs-literal": "text-warning-text",
  "hljs-symbol": "text-warning-text",
  "hljs-bullet": "text-warning-text",
  "hljs-comment": "text-muted-foreground italic",
  "hljs-quote": "text-muted-foreground italic",
  "hljs-meta": "text-muted-foreground",
  "hljs-title": "text-info-text",
  "hljs-attr": "text-info-text",
  "hljs-attribute": "text-info-text",
  "hljs-name": "text-info-text",
  "hljs-tag": "text-info-text",
  "hljs-property": "text-info-text",
  "hljs-selector-class": "text-info-text",
  "hljs-selector-id": "text-info-text",
  "hljs-deletion": "text-danger-text",
};

interface HastNode {
  type: string;
  value?: string;
  properties?: { className?: readonly string[] | string };
  children?: readonly HastNode[];
}

/** The part of lowlight the editor uses. */
export interface CodeHighlighter {
  registered(language: string): boolean;
  highlight(language: string, value: string): { children: readonly HastNode[] };
}

export interface HighlightRange {
  from: number;
  to: number;
  class: string;
}

function classOf(node: HastNode): string {
  const names = node.properties?.className;
  const list = Array.isArray(names) ? names : names ? [names as string] : [];
  return list
    .map((name) => TOKEN_CLASS[name])
    .filter(Boolean)
    .join(" ");
}

/** The coloured ranges of `text` (offsets into the text); none for a language it lacks. */
export function highlightRanges(
  highlighter: CodeHighlighter,
  language: string | null | undefined,
  text: string,
): HighlightRange[] {
  if (!language || !text || !highlighter.registered(language)) return [];
  const ranges: HighlightRange[] = [];
  let at = 0;
  const walk = (nodes: readonly HastNode[], inherited: string) => {
    for (const node of nodes) {
      if (node.type === "text") {
        const length = (node.value ?? "").length;
        if (inherited && length > 0) {
          ranges.push({ from: at, to: at + length, class: inherited });
        }
        at += length;
      } else if (node.type === "element") {
        const own = classOf(node);
        walk(node.children ?? [], own || inherited);
      }
    }
  };
  walk(highlighter.highlight(language, text).children, "");
  return ranges;
}

let loading: Promise<CodeHighlighter> | null = null;

/** Loads the highlighter chunk once. */
export function loadHighlighter(): Promise<CodeHighlighter> {
  loading ??= import("lowlight").then(
    ({ createLowlight, common }) =>
      createLowlight(common) as unknown as CodeHighlighter,
  );
  return loading;
}
