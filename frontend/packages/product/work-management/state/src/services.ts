import {
  createContext,
  createElement,
  useContext,
  type ReactNode,
} from "react";
import type { NotrelixClient } from "@notrelix/contracts";
import { createBoardApi } from "./api/board.api";
import { createBoardAccessApi } from "./api/board-access.api";
import { createBoardSettingsApi } from "./api/board-settings.api";
import { createWorkspaceAutomationsApi } from "./api/workspace-automations.api";
import { createCardApi } from "./api/item.api";
import { createGroupApi } from "./api/group.api";
import { createListApi } from "./api/list.api";
import { createColumnApi } from "./api/field.api";
import { createFieldOptionApi } from "./api/field-option.api";
import { createLabelApi } from "./api/label.api";
import { createSavedFilterApi } from "./api/saved-filter.api";
import { createBoardPreferenceApi } from "./api/board-preference.api";
import { createFormApi } from "./api/form.api";
import { createTemplateApi } from "./api/template.api";
import { createChecklistApi } from "./api/checklist.api";
import { createCommentApi } from "./api/item-comments.api";
import { createItemExtrasApi } from "./api/item-extras.api";

export interface WorkManagementServices {
  readonly boards: ReturnType<typeof createBoardApi>;
  readonly boardPreferences: ReturnType<typeof createBoardPreferenceApi>;
  readonly access: ReturnType<typeof createBoardAccessApi>;
  readonly cards: ReturnType<typeof createCardApi>;
  readonly groups: ReturnType<typeof createGroupApi>;
  readonly lists: ReturnType<typeof createListApi>;
  readonly columns: ReturnType<typeof createColumnApi>;
  readonly fieldOptions: ReturnType<typeof createFieldOptionApi>;
  readonly labels: ReturnType<typeof createLabelApi>;
  readonly savedFilters: ReturnType<typeof createSavedFilterApi>;
  readonly forms: ReturnType<typeof createFormApi>;
  readonly templates: ReturnType<typeof createTemplateApi>;
  readonly checklists: ReturnType<typeof createChecklistApi>;
  readonly comments: ReturnType<typeof createCommentApi>;
  readonly itemExtras: ReturnType<typeof createItemExtrasApi>;
  readonly boardSettings: ReturnType<typeof createBoardSettingsApi>;
  readonly workspaceAutomations: ReturnType<
    typeof createWorkspaceAutomationsApi
  >;
}

export interface WorkManagementServicesOptions {
  /** The backend serves the markers, links and percents of a board plan. */
  readonly planProduced?: boolean;
}

export function createWorkManagementServices(
  client: NotrelixClient,
  options: WorkManagementServicesOptions = {},
): WorkManagementServices {
  return {
    boards: createBoardApi(client),
    boardPreferences: createBoardPreferenceApi(client),
    access: createBoardAccessApi(client),
    cards: createCardApi(client),
    groups: createGroupApi(client),
    lists: createListApi(client),
    columns: createColumnApi(client),
    fieldOptions: createFieldOptionApi(client),
    labels: createLabelApi(client),
    savedFilters: createSavedFilterApi(client),
    forms: createFormApi(client),
    templates: createTemplateApi(client),
    checklists: createChecklistApi(client),
    comments: createCommentApi(client),
    itemExtras: createItemExtrasApi(client, {
      planProduced: options.planProduced,
    }),
    boardSettings: createBoardSettingsApi(client),
    workspaceAutomations: createWorkspaceAutomationsApi(client),
  };
}

const WorkManagementServicesContext =
  createContext<WorkManagementServices | null>(null);

export function WorkManagementServicesProvider({
  services,
  children,
}: {
  services: WorkManagementServices;
  children: ReactNode;
}) {
  return createElement(
    WorkManagementServicesContext.Provider,
    { value: services },
    children,
  );
}

export function useWorkManagementServices(): WorkManagementServices {
  const services = useContext(WorkManagementServicesContext);
  if (!services) {
    throw new Error(
      "useWorkManagementServices must be used within WorkManagementServicesProvider",
    );
  }
  return services;
}
