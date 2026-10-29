/**
 * What moving an item to another board will carry over, worked out in the
 * browser so the person sees it before confirming. It mirrors the server's
 * rules (`MoveBoardItemToBoard`): the group with the same title, else the
 * board's default, else its first; the status of the same name, else the first;
 * a value only on a field of the same name and type; a label only by name; an
 * assignee who cannot see the target board is dropped. The server stays
 * authoritative: whatever it answers is what the person is shown afterwards.
 */
export interface MovePreviewSource {
  /** Title of the group the item is in now. */
  readonly groupTitle: string | null;
  /** Name of the item's current status option. */
  readonly statusName: string | null;
  /** Custom fields that hold a value on the item. */
  readonly fields: readonly { readonly name: string; readonly type: string }[];
  readonly labelNames: readonly string[];
  readonly assignees: readonly { readonly id: string; readonly name: string }[];
}

export interface MovePreviewTarget {
  readonly groupTitles: readonly string[];
  /** Title of the board's default group, when it has one. */
  readonly defaultGroupTitle: string | null;
  readonly statusNames: readonly string[];
  readonly fields: readonly { readonly name: string; readonly type: string }[];
  readonly labelNames: readonly string[];
  /**
   * Who can see the board: the listed people, or `null` when everyone in the
   * workspace sees it.
   */
  readonly visibleTo: ReadonlySet<string> | null;
}

export interface MovePreview {
  /** The group the item lands in, and whether its title matched. */
  readonly group: { readonly title: string | null; readonly matched: boolean };
  /** The status the item keeps, and whether it matched by name. */
  readonly status: { readonly name: string | null; readonly matched: boolean };
  readonly lostFields: readonly string[];
  readonly lostLabels: readonly string[];
  readonly lostAssignees: readonly string[];
  /** How many separate things are lost. */
  readonly lostCount: number;
}

const same = (a: string, b: string) => a.toLowerCase() === b.toLowerCase();

export function previewMoveToBoard(
  source: MovePreviewSource,
  target: MovePreviewTarget,
): MovePreview {
  const matchedGroup =
    source.groupTitle !== null
      ? target.groupTitles.find((title) => same(title, source.groupTitle ?? ""))
      : undefined;
  const group = matchedGroup
    ? { title: matchedGroup, matched: true }
    : {
        title: target.defaultGroupTitle ?? target.groupTitles[0] ?? null,
        matched: false,
      };

  const matchedStatus =
    source.statusName !== null
      ? target.statusNames.find((name) => same(name, source.statusName ?? ""))
      : undefined;
  const status = matchedStatus
    ? { name: matchedStatus, matched: true }
    : { name: target.statusNames[0] ?? null, matched: false };

  const lostFields = source.fields
    .filter(
      (field) =>
        !target.fields.some(
          (other) => other.type === field.type && same(other.name, field.name),
        ),
    )
    .map((field) => field.name);
  const lostLabels = source.labelNames.filter(
    (name) => !target.labelNames.some((other) => same(other, name)),
  );
  const lostAssignees = source.assignees
    .filter(
      (person) => target.visibleTo !== null && !target.visibleTo.has(person.id),
    )
    .map((person) => person.name);

  return {
    group,
    status,
    lostFields,
    lostLabels,
    lostAssignees,
    lostCount: lostFields.length + lostLabels.length + lostAssignees.length,
  };
}
