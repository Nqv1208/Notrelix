import { useParams } from "@tanstack/react-router";
import { MessageSquare } from "lucide-react";

export function ChatPage() {
  const { _workspaceId } = useParams({ from: "/workspaces/$workspaceId/chat" });

  return (
    <div className="h-full flex flex-col items-center justify-center p-8">
      <div className="flex flex-col items-center gap-4 text-center max-w-md">
        <div className="size-16 rounded-2xl bg-muted flex items-center justify-center">
          <MessageSquare className="size-8 text-muted-foreground" />
        </div>
        <h1 className="text-xl font-semibold">Chat</h1>
        <p className="text-sm text-muted-foreground">
          Real-time team chat is coming soon. You'll be able to communicate with
          your workspace members directly.
        </p>
      </div>
    </div>
  );
}
