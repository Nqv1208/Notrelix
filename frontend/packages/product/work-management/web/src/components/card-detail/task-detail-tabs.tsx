import {
  Activity,
  Files,
  FileText,
  ListChecks,
  MessageSquareText,
} from "lucide-react";
import { ScrollArea } from "@notrelix/ui-web";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@notrelix/ui-web";
import type { CardDetail } from "@notrelix/work-management-core";
import { TaskActivityTab } from "./task-activity-tab";
import { TaskDetailEmptyState } from "./task-detail-empty-state";
import { TaskFilesTab } from "./task-files-tab";
import { TaskUpdatesTab } from "./task-updates-tab";

export function TaskDetailTabs({ card }: { card: CardDetail }) {
  return (
    <Tabs defaultValue="updates" className="flex min-h-0 flex-1 flex-col gap-0">
      <div className="sticky top-0 z-10 border-b border-border bg-popover px-4 py-2">
        <TabsList className="max-w-full overflow-x-auto">
          <TabsTrigger value="updates">
            <MessageSquareText className="size-4" />
            Updates
          </TabsTrigger>
          <TabsTrigger value="files">
            <Files className="size-4" />
            Files
          </TabsTrigger>
          <TabsTrigger value="activity">
            <Activity className="size-4" />
            Activity Log
          </TabsTrigger>
          <TabsTrigger value="linked-docs">
            <FileText className="size-4" />
            Linked Docs
          </TabsTrigger>
          <TabsTrigger value="subtasks">
            <ListChecks className="size-4" />
            Subtasks
          </TabsTrigger>
        </TabsList>
      </div>

      <TabsContent value="updates" className="m-0 min-h-0 flex-1">
        <ScrollArea className="h-full">
          <TaskUpdatesTab card={card} />
        </ScrollArea>
      </TabsContent>
      <TabsContent value="files" className="m-0 min-h-0 flex-1">
        <ScrollArea className="h-full">
          <TaskFilesTab card={card} />
        </ScrollArea>
      </TabsContent>
      <TabsContent value="activity" className="m-0 min-h-0 flex-1">
        <ScrollArea className="h-full">
          <TaskActivityTab card={card} />
        </ScrollArea>
      </TabsContent>
      <TabsContent value="linked-docs" className="m-0 min-h-0 flex-1">
        <ScrollArea className="h-full">
          <div className="p-4">
            <TaskDetailEmptyState
              icon={FileText}
              title={
                card.linkedPageId ? "Linked doc connected" : "No linked docs"
              }
              description={
                card.linkedPageId
                  ? card.linkedPageId
                  : "Link a workspace doc from the table to keep specs and task execution together."
              }
            />
          </div>
        </ScrollArea>
      </TabsContent>
      <TabsContent value="subtasks" className="m-0 min-h-0 flex-1">
        <ScrollArea className="h-full">
          <div className="p-4">
            <TaskDetailEmptyState
              icon={ListChecks}
              title="No subtasks yet"
              description="Checklist-backed subtasks will appear here as the card checklist API is expanded."
            />
          </div>
        </ScrollArea>
      </TabsContent>
    </Tabs>
  );
}
