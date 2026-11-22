/** Why a page's attachment list or a link registration failed, as the panel words it. */
export type PageFilesFailure =
  "invalid" | "limitReached" | "forbidden" | "notFound" | "network" | "failed";

/**
 * Maps what the API answered (an `AppError`) to the reason the Files panel
 * shows. A dropped connection is its own reason (the member retries when back
 * online); there is no separate offline state.
 */
export function classifyPageFilesFailure(error: unknown): PageFilesFailure {
  const { status, kind } =
    typeof error === "object" && error !== null
      ? (error as { status?: unknown; kind?: unknown })
      : {};
  if (status === 400) return "invalid";
  if (status === 409) return "limitReached";
  if (status === 403) return "forbidden";
  if (status === 404) return "notFound";
  if (kind === "network") return "network";
  return "failed";
}
