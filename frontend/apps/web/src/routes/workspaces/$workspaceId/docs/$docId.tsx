import { useParams } from '@tanstack/react-router';
import { createDocPageScreen } from '@notrelix/docs-web';
import { api, endpoints } from '@notrelix/contracts';

const DocPageScreen = createDocPageScreen({ api, endpoints });

export function DocPage() {
  const { workspaceId, docId } = useParams({
    from: '/workspaces/$workspaceId/docs/$docId',
  });

  return (
    <DocPageScreen
      workspaceId={workspaceId}
      pageId={docId}
    />
  );
}
