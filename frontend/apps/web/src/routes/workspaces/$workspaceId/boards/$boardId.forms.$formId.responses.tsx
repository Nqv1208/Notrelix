import { useNavigate, useParams } from "@tanstack/react-router";
import { useCurrentUser } from "@notrelix/features-auth";
import { FormInboxPage } from "@notrelix/work-management-web";

const ROUTE =
  "/workspaces/$workspaceId/boards/$boardId/forms/$formId/responses";

/** The response inbox of a form: whoever can edit the board reads and decides. */
export function BoardFormInboxRoutePage() {
  const { workspaceId, boardId, formId } = useParams({ from: ROUTE });
  const navigate = useNavigate({ from: ROUTE });
  const user = useCurrentUser();

  return (
    <FormInboxPage
      workspaceId={workspaceId}
      boardId={boardId}
      formId={formId}
      currentUserId={user?.id ?? null}
      deciderName={user?.name}
      onOpenItem={(itemId) =>
        void navigate({
          to: "/workspaces/$workspaceId/boards/$boardId/items/$itemId",
          params: { workspaceId, boardId, itemId },
          search: {},
        })
      }
      onBack={() =>
        void navigate({
          to: "/workspaces/$workspaceId/boards/$boardId/settings",
          params: { workspaceId, boardId },
          search: { section: "forms" },
        })
      }
      onBackToBoard={() =>
        void navigate({
          to: "/workspaces/$workspaceId/boards/$boardId",
          params: { workspaceId, boardId },
        })
      }
    />
  );
}
