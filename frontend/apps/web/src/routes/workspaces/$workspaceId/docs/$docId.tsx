import { useMemo } from 'react';
import { useParams } from '@tanstack/react-router';
import { createDocPageScreen } from '@notrelix/docs-web';
import { useFeatureRuntimeDependencies } from '@notrelix/runtime-web';

export function DocPage() {
  const { workspaceId, docId } = useParams({
    from: '/workspaces/$workspaceId/docs/$docId',
  });
  const { api, endpoints } = useFeatureRuntimeDependencies();

  const DocPageScreen = useMemo(
    () => createDocPageScreen({ api, endpoints }),
    [api, endpoints],
  );

  return (
    <DocPageScreen
      workspaceId={workspaceId}
      pageId={docId}
    />
  );
}
