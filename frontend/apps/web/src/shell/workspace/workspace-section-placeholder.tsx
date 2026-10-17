import type { LucideIcon } from "lucide-react";
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle,
} from "@notrelix/ui-web";

/** A workspace section page whose content is not delivered yet. */
export function WorkspaceSectionPlaceholder({
  icon: Icon,
  title,
  description,
  emptyTitle,
  emptyDescription,
}: {
  icon: LucideIcon;
  title: string;
  description: string;
  emptyTitle: string;
  emptyDescription: string;
}) {
  return (
    <div className="mx-auto flex w-full max-w-4xl flex-col gap-6 px-4 py-6 md:px-8 md:py-8">
      <header className="flex flex-col gap-1">
        <h1 className="text-page-title text-foreground">{title}</h1>
        <p className="text-body text-muted-foreground">{description}</p>
      </header>
      <Empty className="border border-dashed border-border bg-surface-panel">
        <EmptyHeader>
          <EmptyMedia variant="icon">
            <Icon aria-hidden="true" className="text-icon" />
          </EmptyMedia>
          <EmptyTitle className="text-subheading">{emptyTitle}</EmptyTitle>
          <EmptyDescription className="text-body">
            {emptyDescription}
          </EmptyDescription>
        </EmptyHeader>
      </Empty>
    </div>
  );
}
