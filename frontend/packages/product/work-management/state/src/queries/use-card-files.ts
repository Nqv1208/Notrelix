import { useQuery } from "@tanstack/react-query";
import { wmQueryKeys } from "./keys";
import { useWorkManagementServices } from "../services";

/** Why the item's files could not be read: a refusal (403), the item gone (404), or any other failure. */
export function cardFilesFailure(
  error: unknown,
): "forbidden" | "notFound" | "failed" {
  const status =
    typeof error === "object" && error !== null
      ? (error as { status?: unknown }).status
      : undefined;
  return status === 403 ? "forbidden" : status === 404 ? "notFound" : "failed";
}

export function useCardFiles(cardId: string, workspaceId: string) {
  const { cards } = useWorkManagementServices();
  return useQuery({
    queryKey: wmQueryKeys.cardFiles(workspaceId, cardId),
    queryFn: () => cards.getCardFiles(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  });
}
