import { useParams } from '@tanstack/react-router';

export function InvitePage() {
  const { token } = useParams({ from: '/invite/$token' });

  return (
    <div className="min-h-screen flex items-center justify-center p-8">
      <div className="w-full max-w-md text-center">
        <h1 className="text-3xl font-bold mb-4">Accept Invitation</h1>
        <p className="text-muted-foreground mb-8">
          Processing invitation token: {token}
        </p>
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto" />
      </div>
    </div>
  );
}
