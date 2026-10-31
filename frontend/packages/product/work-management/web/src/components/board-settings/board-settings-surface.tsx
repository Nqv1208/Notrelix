import type { ReactNode } from "react";
import {
  AlignLeft,
  Archive,
  ChevronLeft,
  Eye,
  ClipboardList,
  LayoutTemplate,
  Lock,
  Sun,
  Tag,
  Table2,
  TriangleAlert,
  Zap,
  type LucideIcon,
} from "lucide-react";
import { Skeleton, cn } from "@notrelix/ui-web";
import { PageLink } from "../item-page/item-page-parts";
import { AutomationEditor } from "./automation-editor";
import { AutomationLog } from "./automation-log";
import { AutomationsSection } from "./board-settings-automations";
import { WorkspaceRulesSection } from "./workspace-rules-section";
import { BoardSettingsProvider, useSettings } from "./board-settings-context";
import { FieldsSection } from "./board-settings-fields";
import { fillCopy } from "./board-settings-format";
import { GeneralSection } from "./board-settings-general";
import { GroupsSection } from "./board-settings-groups";
import { LabelsSection } from "./board-settings-labels";
import { FormsListPanel } from "../forms/forms-list-panel";
import { TemplatesSection } from "./board-settings-templates";
import { BUTTON_CLASS, Callout } from "./board-settings-parts";
import {
  BOARD_SETTINGS_SECTIONS,
  type BoardSettingsSection,
  type BoardSettingsSurfaceProps,
} from "./board-settings-types";
import { ViewsSection } from "./board-settings-views";

const MARK_TONE: Readonly<Record<string, string>> = {
  blue: "bg-mark-blue text-mark-blue-foreground",
  gold: "bg-mark-gold text-mark-gold-foreground",
  coral: "bg-mark-coral text-mark-coral-foreground",
  sage: "bg-mark-sage text-mark-sage-foreground",
  crimson: "bg-mark-crimson text-mark-crimson-foreground",
  violet: "bg-mark-violet text-mark-violet-foreground",
};

const SECTION_ICON: Readonly<Record<BoardSettingsSection, LucideIcon>> = {
  general: Sun,
  fields: Table2,
  views: Eye,
  groups: AlignLeft,
  labels: Tag,
  forms: ClipboardList,
  templates: LayoutTemplate,
  automations: Zap,
};

/**
 * Board settings: the page a manager of the board opens from "Thiết lập".
 * A header with the place and the way back, a menu of sections (general,
 * fields, views, groups, access, automations, archived) and the section's
 * content, with a panel on the right for a field or a view. Anyone else gets
 * the no-access card. Pure: data, copy and callbacks are injected; every
 * write goes through `actions`, which answer whether the change went through.
 */
export function BoardSettingsSurface(props: BoardSettingsSurfaceProps) {
  const { copy, status, settings } = props;
  const forbidden = status === "forbidden";

  return (
    <div className="flex h-full min-h-0 min-w-0 flex-1 flex-col bg-background text-foreground">
      <header className="flex flex-wrap items-center gap-3 border-b border-line px-6 pt-4 pb-3.5">
        {props.space ? (
          <span
            aria-hidden="true"
            className={cn(
              "grid size-[34px] shrink-0 place-items-center rounded-[9px] text-mark-lg font-bold",
              MARK_TONE[props.space.mark.tone],
            )}
          >
            {props.space.mark.initials}
          </span>
        ) : null}
        <div>
          <nav
            aria-label={copy.header.crumb}
            className="text-crumb text-meta-foreground"
          >
            <PageLink
              href={props.boardHref}
              onNavigate={props.onBack}
              className="font-medium text-brand-subtle-foreground hover:underline"
            >
              {props.boardTitle}
            </PageLink>{" "}
            / <span aria-current="page">{copy.header.settings}</span>
          </nav>
          <h1 className="m-0 text-[21px] leading-[1.3] font-[650] tracking-[-0.01em]">
            {copy.title}
          </h1>
        </div>
        <span className="flex-1" />
        {forbidden ? null : (
          <span className="text-item-small text-meta-foreground">
            {copy.header.hint}
          </span>
        )}
        <PageLink
          href={props.boardHref}
          onNavigate={props.onBack}
          className={BUTTON_CLASS.outline}
        >
          <ChevronLeft aria-hidden="true" />
          {copy.header.back}
        </PageLink>
      </header>

      <div className="flex min-h-0 flex-1">
        {status === "forbidden" ? (
          <DeniedCard {...props} />
        ) : status === "loading" || (status === "ready" && !settings) ? (
          <LoadingBody label={copy.loading} />
        ) : status === "error" || !settings ? (
          <ErrorBody {...props} />
        ) : (
          <BoardSettingsProvider
            copy={copy}
            settings={settings}
            actions={props.actions}
            timeZone={props.timeZone}
            defaultFeedback={props.defaultFeedback}
            onReload={props.onReload}
          >
            <ReadyBody {...props} />
          </BoardSettingsProvider>
        )}
      </div>
    </div>
  );
}

function DeniedCard({
  copy,
  askName,
  onBack,
  boardHref,
}: BoardSettingsSurfaceProps) {
  return (
    <section
      aria-labelledby="bs-denied-h"
      className="grid flex-1 place-items-center p-6"
    >
      <div className="flex max-w-[420px] flex-col items-center gap-3 text-center">
        <span
          aria-hidden="true"
          className="grid size-11 place-items-center rounded-xl border border-line bg-popover text-muted-foreground"
        >
          <Lock className="size-4" />
        </span>
        <h2
          id="bs-denied-h"
          className="m-0 text-[17px] leading-[1.45] font-[650]"
        >
          {copy.denied.title}
        </h2>
        <p className="m-0 text-item-ui leading-[1.55] text-muted-foreground">
          {askName
            ? fillCopy(copy.denied.ask, { name: askName })
            : copy.denied.askAnyone}{" "}
          {copy.denied.rest}
        </p>
        <PageLink
          href={boardHref}
          onNavigate={onBack}
          className={BUTTON_CLASS.outline}
        >
          <ChevronLeft aria-hidden="true" />
          {copy.header.back}
        </PageLink>
      </div>
    </section>
  );
}

function LoadingBody({ label }: { label: string }) {
  return (
    <div
      role="status"
      aria-label={label}
      className="flex min-w-0 flex-1 gap-6 p-6"
    >
      <div className="flex w-[188px] flex-col gap-2">
        {Array.from({ length: 6 }, (_, index) => (
          <Skeleton key={index} className="h-[34px] rounded-[7px]" />
        ))}
      </div>
      <div className="flex min-w-0 flex-1 flex-col gap-3">
        <Skeleton className="h-6 w-48" />
        <Skeleton className="h-5 w-[60ch] max-w-full" />
        <Skeleton className="mt-4 h-72 rounded-[10px]" />
      </div>
    </div>
  );
}

function ErrorBody({ copy, onReload }: BoardSettingsSurfaceProps) {
  return (
    <section role="alert" className="grid flex-1 place-items-center p-6">
      <div className="flex max-w-[420px] flex-col items-center gap-3 text-center">
        <span
          aria-hidden="true"
          className="grid size-11 place-items-center rounded-xl border border-line bg-popover text-danger-text"
        >
          <TriangleAlert className="size-4" />
        </span>
        <h2 className="m-0 text-[17px] leading-[1.45] font-[650]">
          {copy.error.title}
        </h2>
        <p className="m-0 text-item-ui text-muted-foreground">
          {copy.error.body}
        </p>
        <button
          type="button"
          onClick={onReload}
          className={BUTTON_CLASS.outline}
        >
          {copy.error.retry}
        </button>
      </div>
    </section>
  );
}

function ReadyBody(props: BoardSettingsSurfaceProps) {
  const { copy, settings, onSelectSection } = props;
  const { feedback, failure, clearFeedback } = useSettings();
  const data = settings!;
  const unavailable = data.unavailable ?? {};
  const hidden: Partial<Record<BoardSettingsSection, boolean>> = {
    views: unavailable.views,
    labels: !props.labels,
    forms: !props.forms,
    templates: !props.templates,
    automations: unavailable.automations && !props.workspaceRules,
  };
  // A section the product has no contract for is not opened by its address.
  const section = hidden[props.section] ? "general" : props.section;
  const menu = BOARD_SETTINGS_SECTIONS.filter(
    (key) => key !== "automations" && !hidden[key],
  );
  const t = copy.menu;
  const rules = props.workspaceRules;
  const running = rules
    ? rules.status === "ready"
      ? rules.rules.filter((rule) => rule.enabled).length
      : undefined
    : data.automations.filter((rule) => rule.enabled).length;
  const counts: Partial<Record<BoardSettingsSection, number>> = {
    fields: data.fields.length + data.systemFields.length,
    views: data.views.length,
    groups: data.groups.length,
    labels:
      props.labels?.status === "ready" ? props.labels.labels.length : undefined,
    forms:
      props.forms?.status === "ready" ? props.forms.forms.length : undefined,
    templates:
      props.templates?.status === "ready"
        ? props.templates.templates.length
        : undefined,
    automations: running,
  };

  // "Đã lưu trữ" is the archived part of the fields section.
  function openArchived() {
    onSelectSection("fields");
    requestAnimationFrame(() =>
      document
        .getElementById("bs-archived")
        ?.scrollIntoView?.({ block: "start" }),
    );
  }

  const linkClass = (on: boolean) =>
    cn(
      "flex h-[34px] w-full cursor-pointer items-center gap-[9px] rounded-[7px] border-0 bg-transparent px-2.5 text-left text-nav font-medium text-muted-foreground outline-none hover:text-foreground focus-visible:ring-2 focus-visible:ring-focus-ring [&_svg]:size-4 [&_svg]:shrink-0",
      on &&
        "bg-popover font-semibold text-foreground shadow-[0_0_0_1px_var(--line)]",
    );
  const count = (value?: number) =>
    value === undefined ? null : (
      <span className="ml-auto text-metadata font-medium text-meta-foreground">
        {value}
      </span>
    );

  let content: ReactNode;
  if (section === "general")
    content = <GeneralSection onLeave={props.onLeave} space={props.space} />;
  else if (section === "views")
    content = (
      <ViewsSection
        viewId={props.viewId}
        onSelectView={props.onSelectView}
        kanbanSettings={props.kanbanSettings}
      />
    );
  else if (section === "groups") content = <GroupsSection />;
  else if (section === "labels" && props.labels)
    content = <LabelsSection panel={props.labels} />;
  else if (section === "forms" && props.forms)
    content = <FormsListPanel {...props.forms} />;
  else if (section === "templates" && props.templates)
    content = <TemplatesSection panel={props.templates} />;
  else if (section === "automations" && props.workspaceRules)
    content = <WorkspaceRulesSection panel={props.workspaceRules} />;
  else if (section === "automations") {
    const panel = props.automation;
    if (panel?.log)
      content = (
        <AutomationLog
          copy={copy.automations.editor}
          panel={panel}
          timeZone={props.timeZone}
          billingHref={panel.billingHref}
        />
      );
    else if (panel?.ruleId)
      content = (
        <AutomationEditor
          copy={copy.automations.editor}
          panel={panel}
          timeZone={props.timeZone}
          scopeMark={
            props.space ? (
              <span
                aria-hidden="true"
                className={cn(
                  "grid size-7 shrink-0 place-items-center rounded-lg text-mark-sm font-bold",
                  MARK_TONE[props.space.mark.tone],
                )}
              >
                {props.space.mark.initials}
              </span>
            ) : null
          }
        />
      );
    else content = <AutomationsSection panel={panel} />;
  } else
    content = (
      <FieldsSection
        fieldId={props.fieldId}
        onSelectField={props.onSelectField}
        defaultPickerOpen={props.defaultPickerOpen}
        defaultArchiveFieldId={props.defaultArchiveFieldId}
        deleted={props.deletedFields}
      />
    );

  return (
    <>
      <nav
        aria-label={t.aria}
        className="flex w-[212px] shrink-0 flex-col gap-0.5 border-r border-line px-3 py-4"
      >
        {menu.map((key) => {
          const Icon = SECTION_ICON[key];
          return (
            <button
              key={key}
              type="button"
              aria-current={section === key ? "page" : undefined}
              onClick={() => onSelectSection(key)}
              className={linkClass(section === key)}
            >
              <Icon aria-hidden="true" />
              {t[key]}
              {count(counts[key])}
            </button>
          );
        })}
        <div aria-hidden="true" className="mx-1.5 my-2 h-px bg-line" />
        <button
          type="button"
          onClick={props.onOpenAccess}
          className={linkClass(false)}
        >
          <Lock aria-hidden="true" />
          {t.access}
        </button>
        {hidden.automations ? null : (
          <button
            type="button"
            aria-current={section === "automations" ? "page" : undefined}
            onClick={() => onSelectSection("automations")}
            className={linkClass(section === "automations")}
          >
            <Zap aria-hidden="true" />
            {t.automations}
            {count(counts.automations)}
          </button>
        )}
        {unavailable.fieldArchive ? null : (
          <button
            type="button"
            onClick={openArchived}
            className={linkClass(false)}
          >
            <Archive aria-hidden="true" />
            {t.archived}
            {count(data.archivedFields.length)}
          </button>
        )}
      </nav>
      <div className="relative flex min-h-0 min-w-0 flex-1">
        {content}
        {feedback ? (
          <div className="absolute top-3 left-1/2 z-10 w-[min(520px,calc(100%-32px))] -translate-x-1/2">
            <Callout
              icon={TriangleAlert}
              tone="warning"
              role="alert"
              className="shadow-popover"
            >
              <span className="flex flex-1 flex-col gap-1">
                <b className="font-semibold">
                  {feedback === "conflict"
                    ? copy.feedback.conflictTitle
                    : feedback === "invalid"
                      ? copy.feedback.invalid
                      : copy.feedback.failed}
                </b>
                {feedback === "conflict" ? (
                  <span>{copy.feedback.conflictBody}</span>
                ) : null}
                <span className="mt-1 flex gap-2">
                  {failure?.retry ? (
                    <button
                      type="button"
                      onClick={failure.retry}
                      className={cn(BUTTON_CLASS.outline, "h-7")}
                    >
                      {copy.feedback.retry}
                    </button>
                  ) : null}
                  <button
                    type="button"
                    onClick={clearFeedback}
                    className={cn(BUTTON_CLASS.ghost, "h-7")}
                  >
                    {copy.common.close}
                  </button>
                </span>
              </span>
            </Callout>
          </div>
        ) : null}
      </div>
    </>
  );
}
