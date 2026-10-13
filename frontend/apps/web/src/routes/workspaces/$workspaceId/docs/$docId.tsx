import { useMemo } from "react";
import { useParams } from "@tanstack/react-router";
import { createDocPageScreen } from "@notrelix/docs-web";
import { useFeatureRuntimeDependencies } from "@notrelix/runtime-web";
import { useDocPageHost } from "@/shell/docs/use-doc-page-host";

export function DocPage() {
  const { workspaceId, docId } = useParams({
    from: "/workspaces/$workspaceId/docs/$docId",
  });
  const { api, endpoints } = useFeatureRuntimeDependencies();

  const DocPageScreen = useMemo(
    () => createDocPageScreen({ api, endpoints, useHost: useDocPageHost }),
    [api, endpoints],
  );

  return <DocPageScreen workspaceId={workspaceId} pageId={docId} />;
}
