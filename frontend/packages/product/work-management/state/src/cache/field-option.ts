import type {
  FieldDefinition,
  FieldOption,
  FullBoardResponse,
} from "@notrelix/work-management-core";

/** The field of a board read, or `undefined`. */
export function findBoardField(
  fullBoard: FullBoardResponse | undefined,
  fieldId: string,
): FieldDefinition | undefined {
  return fullBoard?.board.fieldDefinitions.find(
    (field) => field.id === fieldId,
  );
}

function withField(
  fullBoard: FullBoardResponse,
  fieldId: string,
  change: (field: FieldDefinition) => FieldDefinition,
): FullBoardResponse {
  return {
    ...fullBoard,
    board: {
      ...fullBoard.board,
      fieldDefinitions: fullBoard.board.fieldDefinitions.map((field) =>
        field.id === fieldId ? change(field) : field,
      ),
    },
  };
}

/**
 * Optimistic "add option": the option is the field's last (no position key
 * sorts last), so the lane appears after the existing ones at once. The
 * refetch replaces it with the server's option and id.
 */
export function applyAddedOption(
  fullBoard: FullBoardResponse | undefined,
  fieldId: string,
  option: FieldOption,
): FullBoardResponse | undefined {
  if (!fullBoard || !findBoardField(fullBoard, fieldId)) return fullBoard;
  return withField(fullBoard, fieldId, (field) => ({
    ...field,
    options: [...field.options, option],
  }));
}

/** Optimistic rename / recolour / recategorise of one option. */
export function applyUpdatedOption(
  fullBoard: FullBoardResponse | undefined,
  fieldId: string,
  optionId: string,
  patch: Partial<Pick<FieldOption, "label" | "color" | "category">>,
): FullBoardResponse | undefined {
  if (!fullBoard || !findBoardField(fullBoard, fieldId)) return fullBoard;
  return withField(fullBoard, fieldId, (field) => ({
    ...field,
    options: field.options.map((option) =>
      option.id === optionId ? { ...option, ...patch } : option,
    ),
  }));
}

/** Puts one field back as it was, keeping every other change of the read. */
export function restoreBoardField(
  fullBoard: FullBoardResponse | undefined,
  fieldId: string,
  before: FieldDefinition | undefined,
): FullBoardResponse | undefined {
  if (!fullBoard || !before) return fullBoard;
  return withField(fullBoard, fieldId, () => before);
}
