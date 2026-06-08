import { pageApi } from "./page.api"
import { pageCommentsApi } from "./page-comments.api"
import { pageActivityApi } from "./page-activity.api"

export const pageService = {
  ...pageApi,
  ...pageCommentsApi,
  ...pageActivityApi,
}

