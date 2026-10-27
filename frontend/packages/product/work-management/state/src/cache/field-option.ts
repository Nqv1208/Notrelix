import type {
  Card,
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

/**
 * Optimistic "remove option": the option leaves the field and the cards that
 * held it take the replacement (a Select without one drops the value), as the
 * producer rewrites them. The refetch replaces all of it.
 */
export function applyRemovedOption(
  fullBoard: FullBoardResponse | undefined,
  fieldId: string,
  optionId: string,
  replacementOptionId: string | null,
): FullBoardResponse | undefined {
  const field = findBoardField(fullBoard, fieldId);
  if (!fullBoard || !field) return fullBoard;
  const replacement = replacementOptionId
    ? field.options.find((option) => option.id === replacementOptionId)
    : undefined;
  const isStatus = field.semanticRole === "status";
  const remap = (card: Card): Card => {
    const held =
      card.fieldValues[fieldId] === optionId ||
      (isStatus && card.statusOptionId === optionId);
    if (!held) return card;
    const { [fieldId]: _dropped, ...rest } = card.fieldValues;
    const fieldValues = replacement
      ? { ...card.fieldValues, [fieldId]: replacement.id }
      : rest;
    if (!isStatus) return { ...card, fieldValues };
    return {
      ...card,
      fieldValues,
      statusOptionId: replacement?.id ?? null,
      statusCategory: replacement?.category ?? null,
      status: replacement?.label ?? "",
    };
  };
  const withOptionGone = withField(fullBoard, fieldId, (current) => ({
    ...current,
    options: current.options.filter((option) => option.id !== optionId),
  }));
  return {
    ...withOptionGone,
    groups: withOptionGone.groups.map((group) => ({
      ...group,
      cards: group.cards.map(remap),
    })),
  };
}

/**
 * Optimistic "move option" one place: the option and the neighbour it passes
 * swap their keys, which is exactly the order the producer settles on.
 */
export function applySwappedOptions(
  fullBoard: FullBoardResponse | undefined,
  fieldId: string,
  optionId: string,
  neighbourId: string,
): FullBoardResponse | undefined {
  const field = findBoardField(fullBoard, fieldId);
  if (!fullBoard || !field) return fullBoard;
  const moved = field.options.find((option) => option.id === optionId);
  const neighbour = field.options.find((option) => option.id === neighbourId);
  if (!moved || !neighbour) return fullBoard;
  return withField(fullBoard, fieldId, (current) => ({
    ...current,
    options: current.options.map((option) =>
      option.id === optionId
        ? { ...option, position: neighbour.position }
        : option.id === neighbourId
          ? { ...option, position: moved.position }
          : option,
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
