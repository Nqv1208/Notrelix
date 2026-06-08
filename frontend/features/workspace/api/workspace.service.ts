import { activityApi } from "./activity.api"
import { invitationsApi } from "./invitations.api"
import { membersApi } from "./members.api"
import { viewsApi } from "./views.api"
import { workspaceApi } from "./workspace.api"

export const workspaceService = {
  ...workspaceApi,
  ...membersApi,
  ...invitationsApi,
  ...activityApi,
  ...viewsApi,
}
