import { createContext, createElement, useContext, type ReactNode } from "react"
import type { NotrelixClient } from "@notrelix/contracts"
import { createBoardApi } from "./api/board.api"
import { createCardApi } from "./api/item.api"
import { createGroupApi } from "./api/group.api"
import { createListApi } from "./api/list.api"
import { createColumnApi } from "./api/field.api"
import { createLabelApi } from "./api/label.api"
import { createChecklistApi } from "./api/checklist.api"
import { createCommentApi } from "./api/item-comments.api"

export interface WorkManagementServices {
  readonly boards: ReturnType<typeof createBoardApi>
  readonly cards: ReturnType<typeof createCardApi>
  readonly groups: ReturnType<typeof createGroupApi>
  readonly lists: ReturnType<typeof createListApi>
  readonly columns: ReturnType<typeof createColumnApi>
  readonly labels: ReturnType<typeof createLabelApi>
  readonly checklists: ReturnType<typeof createChecklistApi>
  readonly comments: ReturnType<typeof createCommentApi>
}

export function createWorkManagementServices(client: NotrelixClient): WorkManagementServices {
  return {
    boards: createBoardApi(client),
    cards: createCardApi(client),
    groups: createGroupApi(client),
    lists: createListApi(client),
    columns: createColumnApi(client),
    labels: createLabelApi(client),
    checklists: createChecklistApi(client),
    comments: createCommentApi(client),
  }
}

const WorkManagementServicesContext = createContext<WorkManagementServices | null>(null)

export function WorkManagementServicesProvider({
  services,
  children,
}: {
  services: WorkManagementServices
  children: ReactNode
}) {
  return createElement(WorkManagementServicesContext.Provider, { value: services }, children)
}

export function useWorkManagementServices(): WorkManagementServices {
  const services = useContext(WorkManagementServicesContext)
  if (!services) {
    throw new Error("useWorkManagementServices must be used within WorkManagementServicesProvider")
  }
  return services
}
