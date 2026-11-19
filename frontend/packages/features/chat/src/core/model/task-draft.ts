/** The longest task title a message gives (the dialog lets the person edit it). */
export const TASK_TITLE_MAX = 120;

/** A task made from a chat message (UC-CHT-I08): what the host's create dialog opens with. */
export interface ChatTaskDraft {
  /** The first line of the message, cut at TASK_TITLE_MAX. */
  readonly title: string;
  /** The message text as it was when the task was made, then a link back to the message. Markdown. */
  readonly descriptionMd: string;
  /** The Board the open channel is linked to, or null when the person has to pick one. */
  readonly boardId: string | null;
}

/**
 * The title and description a task starts with. The description keeps the whole text as a snapshot, so the task still reads the
 * same when the message is deleted later, and ends with a link back to the message (when the host can build one).
 */
export function taskDraftFromMessage(input: {
  readonly text: string;
  readonly link: string | null;
  /** The words of the link back, such as "Source: #general". */
  readonly linkLabel: string;
  readonly boardId: string | null;
}): ChatTaskDraft {
  const text = input.text.trim();
  const firstLine =
    text
      .split(/\r?\n/)
      .map((line) => line.trim())
      .find((line) => line !== "") ?? "";
  const chars = Array.from(firstLine);
  const title =
    chars.length > TASK_TITLE_MAX
      ? `${chars
          .slice(0, TASK_TITLE_MAX - 1)
          .join("")
          .trimEnd()}…`
      : firstLine;
  const back = input.link ? `[${input.linkLabel}](${input.link})` : "";
  return {
    title,
    descriptionMd: [text, back].filter((part) => part !== "").join("\n\n"),
    boardId: input.boardId,
  };
}
