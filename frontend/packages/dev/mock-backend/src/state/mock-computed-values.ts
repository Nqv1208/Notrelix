/**
 * Formula results of the mock world, evaluated on every board and item read
 * like the producer does (never stored). Supports the arithmetic subset of
 * the formula language the mock boards use: `[Field name]` references with
 * `+ - * /` and parentheses. A reference to a field with no value gives an
 * empty result, a non-number gives `type`, and a division by zero gives
 * `divide-by-zero`.
 */
export type MockComputedValue =
  | { state: "ok"; value: number }
  | { state: "empty" }
  | { state: "error"; errorCode: string };

interface ColumnLike {
  readonly id: string;
  readonly name: string;
  readonly fieldType: string;
  readonly settings?: string;
}

function expressionOf(column: ColumnLike): string | null {
  try {
    const parsed = JSON.parse(column.settings ?? "{}") as {
      expression?: unknown;
    };
    return typeof parsed.expression === "string" ? parsed.expression : null;
  } catch {
    return null;
  }
}

function evaluate(
  expression: string,
  columns: readonly ColumnLike[],
  values: Readonly<Record<string, unknown>>,
): MockComputedValue {
  let empty = false;
  let wrongType = false;
  const arithmetic = expression.replace(/\[([^\]]+)\]/g, (_match, name) => {
    const column = columns.find((candidate) => candidate.name === name);
    const value = column ? values[column.id] : undefined;
    if (value === undefined || value === null || value === "") {
      empty = true;
      return "0";
    }
    if (typeof value !== "number") {
      wrongType = true;
      return "0";
    }
    return `(${value})`;
  });
  if (wrongType) return { state: "error", errorCode: "type" };
  if (empty) return { state: "empty" };
  if (!/^[\d\s+\-*/().]+$/.test(arithmetic))
    return { state: "error", errorCode: "invalid-definition" };
  const result = calculate(arithmetic);
  if (result === null)
    return { state: "error", errorCode: "invalid-definition" };
  return Number.isFinite(result)
    ? { state: "ok", value: result }
    : { state: "error", errorCode: "divide-by-zero" };
}

/** `+ - * /` and parentheses over numbers; null when the text is not an expression. */
function calculate(text: string): number | null {
  const tokens = text.match(/\d+(?:\.\d+)?|[()+\-*/]/g) ?? [];
  let at = 0;
  const primary = (): number | null => {
    const token = tokens[at++];
    if (token === "(") {
      const inner = sum();
      return tokens[at++] === ")" ? inner : null;
    }
    if (token === "-") {
      const next = primary();
      return next === null ? null : -next;
    }
    const number = Number(token);
    return token !== undefined && !Number.isNaN(number) ? number : null;
  };
  const product = (): number | null => {
    let left = primary();
    while (left !== null && (tokens[at] === "*" || tokens[at] === "/")) {
      const operator = tokens[at++];
      const right = primary();
      if (right === null) return null;
      left = operator === "*" ? left * right : left / right;
    }
    return left;
  };
  function sum(): number | null {
    let left = product();
    while (left !== null && (tokens[at] === "+" || tokens[at] === "-")) {
      const operator = tokens[at++];
      const right = product();
      if (right === null) return null;
      left = operator === "+" ? left + right : left - right;
    }
    return left;
  }
  const result = sum();
  return at === tokens.length ? result : null;
}

/** The Formula results of one item, by field id. */
export function projectComputedValues(
  columns: readonly ColumnLike[],
  values: Readonly<Record<string, unknown>>,
): Record<string, MockComputedValue> {
  const result: Record<string, MockComputedValue> = {};
  for (const column of columns) {
    if (column.fieldType.trim().toLowerCase() !== "formula") continue;
    const expression = expressionOf(column);
    result[column.id] = expression
      ? evaluate(expression, columns, values)
      : { state: "error", errorCode: "invalid-definition" };
  }
  return result;
}
