import { useState } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import { FormBuilderPage } from "@notrelix/work-management-web";

const ROUTE = "/workspaces/$workspaceId/boards/$boardId/forms/$formId/edit";

/** The form builder of a board's manager: reached from "Thiết lập board › Biểu mẫu". */
export function BoardFormBuilderRoutePage() {
  const { workspaceId, boardId, formId } = useParams({ from: ROUTE });
  const navigate = useNavigate({ from: ROUTE });
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [preview, setPreview] = useState(false);

  return (
    <FormBuilderPage
      workspaceId={workspaceId}
      boardId={boardId}
      formId={formId}
      selectedId={selectedId}
      onSelect={setSelectedId}
      preview={preview}
      onPreview={setPreview}
      onBack={() =>
        void navigate({
          to: "/workspaces/$workspaceId/boards/$boardId/settings",
          params: { workspaceId, boardId },
          search: { section: "forms" },
        })
      }
    />
  );
}
