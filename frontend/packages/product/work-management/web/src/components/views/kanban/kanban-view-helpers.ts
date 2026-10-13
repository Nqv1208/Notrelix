/**
 * Card link for the clipboard: the current board path with the `item`
 * parameter set. Other parameters are kept; `view=kanban` is added when the
 * URL has no view.
 */
export function buildCardLink(
  location: Pick<Location, "origin" | "pathname" | "search">,
  cardId: string,
): string {
  const params = new URLSearchParams(location.search);
  if (!params.has("view")) params.set("view", "kanban");
  params.set("item", cardId);
  return `${location.origin}${location.pathname}?${params.toString()}`;
}
