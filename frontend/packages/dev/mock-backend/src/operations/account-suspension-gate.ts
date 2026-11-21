import type { MockHttpResult } from "../transport/create-response";
import type { NormalizedMockRequest } from "../transport/normalize-request";
import type { MockStore } from "../state/mock-store";

const WRITES = new Set(["POST", "PUT", "PATCH", "DELETE"]);

/**
 * The producer refuses every write of a Suspended or Closed Account with 403 `account.suspended`; reads stay
 * open. This is the one place the mock does the same, before any handler runs. The workspace is named by the
 * route (`:workspaceId`) or by the board it addresses (`:boardId`).
 *
 * shortcut: routes that name neither (item, page or form ids alone) are not gated, extend the lookup when a
 * screen under test needs it.
 */
export function refuseWriteOfSuspendedAccount(
  request: NormalizedMockRequest,
  params: Readonly<Record<string, string>>,
  store: MockStore,
): MockHttpResult<unknown> | null {
  if (!WRITES.has(request.method)) return null;
  const workspaceId =
    params.workspaceId ??
    (params.boardId ? store.getBoard(params.boardId)?.workspaceId : undefined);
  if (!workspaceId || store.accountStatusOf(workspaceId) === "Active")
    return null;
  const detail = "The Account is suspended. Changes are paused.";
  return {
    status: 403,
    body: {
      status: 403,
      title: detail,
      detail,
      message: detail,
      errorCode: "account.suspended",
      code: "account.suspended",
    },
  };
}
