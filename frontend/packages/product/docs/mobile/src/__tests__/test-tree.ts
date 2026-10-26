import React, { type ReactElement, type ReactNode } from "react";

export interface Seen {
  readonly type: string;
  readonly props: Record<string, unknown>;
  readonly children: (Seen | string)[];
}

/**
 * Resolves a React element tree to host elements without a renderer: function
 * components are called, host elements (View, Text, ...) are kept. Only valid
 * for components that use no hooks.
 */
export function expand(node: ReactNode): (Seen | string)[] {
  if (node === null || node === undefined || typeof node === "boolean")
    return [];
  if (typeof node === "string" || typeof node === "number")
    return [String(node)];
  if (Array.isArray(node))
    return node.flatMap((child) => expand(child as ReactNode));
  const element = node as ReactElement<Record<string, unknown>>;
  if (typeof element.type === "function") {
    const component = element.type as (props: unknown) => ReactNode;
    return expand(component(element.props));
  }
  const children = expand(element.props.children as ReactNode);
  const type =
    typeof element.type === "string"
      ? element.type
      : ((element.type as { name?: string }).name ?? "host");
  return [{ type, props: element.props, children }];
}

export function textOf(nodes: readonly (Seen | string)[]): string {
  return nodes
    .map((n) => (typeof n === "string" ? n : textOf(n.children)))
    .join("");
}

export function findAll(
  nodes: readonly (Seen | string)[],
  match: (seen: Seen) => boolean,
): Seen[] {
  return nodes.flatMap((n) =>
    typeof n === "string"
      ? []
      : [...(match(n) ? [n] : []), ...findAll(n.children, match)],
  );
}

export const h = React.createElement;
