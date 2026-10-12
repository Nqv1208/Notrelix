import type { NotrelixClient } from "@notrelix/contracts";
import { endpoints } from "@notrelix/contracts";
import type {
  BoardAccess,
  GeneratedBoardAccessDto,
} from "@notrelix/work-management-core";
import { mapBoardAccessDto } from "@notrelix/work-management-core";

/**
 * The caller's access to one board and who else can open it
 * (`WorkManagement.Boards.GetAccess`). A board the caller cannot read answers
 * NotFound, exactly like a board that does not exist.
 */
export function createBoardAccessApi(client: NotrelixClient) {
  const api = client.api;
  return {
    async getBoardAccess(boardId: string): Promise<BoardAccess> {
      const dto = await api.get<GeneratedBoardAccessDto>(
        endpoints.boards.access(boardId),
      );
      return mapBoardAccessDto(dto);
    },
  };
}
