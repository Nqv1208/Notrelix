import { useParams } from '@tanstack/react-router';

export function DocPage() {
  const { workspaceId, docId } = useParams({
    from: '/workspaces/$workspaceId/docs/$docId',
  });

  return (
    <div className="p-8">
      <h1 className="text-3xl font-bold mb-4">Document</h1>
      <p className="text-muted-foreground">
        Workspace: {workspaceId}, Document: {docId}
      </p>
    </div>
  );
}
