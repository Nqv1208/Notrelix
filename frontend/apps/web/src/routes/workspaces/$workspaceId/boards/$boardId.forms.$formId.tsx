import { useNavigate, useParams } from "@tanstack/react-router";
import { useCurrentUser } from "@notrelix/features-auth";
import { FormFillPage } from "@notrelix/work-management-web";

const ROUTE = "/workspaces/$workspaceId/boards/$boardId/forms/$formId";

/**
 * The page a member opens from a form's link: in the app, signed in. The
 * answers go to the form's inbox; a manager turns them into an item.
 */
export function BoardFormFillPage() {
  const { workspaceId, boardId, formId } = useParams({ from: ROUTE });
  const navigate = useNavigate({ from: ROUTE });
  const user = useCurrentUser();

  return (
    <FormFillPage
      workspaceId={workspaceId}
      boardId={boardId}
      formId={formId}
      user={user ? { name: user.name, email: user.email } : undefined}
      onBack={() =>
        void navigate({
          to: "/workspaces/$workspaceId/boards/$boardId",
          params: { workspaceId, boardId },
          search: { view: "kanban" },
        })
      }
      onHome={() =>
        void navigate({
          to: "/workspaces/$workspaceId",
          params: { workspaceId },
        })
      }
    />
  );
}
