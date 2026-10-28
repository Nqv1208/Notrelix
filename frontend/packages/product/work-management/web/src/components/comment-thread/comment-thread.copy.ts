/**
 * Words of a comment's replies and reactions, vi and en. Both the item page
 * and the side sheet carry them as their `thread` section, so each catalog
 * stays complete on its own. `{{name}}` placeholders are filled by
 * `fillSheetCopy`; counted messages carry `_one` / `_other` forms.
 */
export const threadEn = {
  reply: "Reply",
  replyTo: "Reply to {{name}}",
  replyPlaceholder: "Write a reply…",
  replySend: "Send reply",
  replyCancel: "Cancel",
  replies: "Replies to {{name}}",
  showMore_one: "Show {{count}} more reply",
  showMore_other: "Show {{count}} more replies",
  addReaction: "Add a reaction",
  pick: "React with {{emoji}}",
  chip: "{{emoji}} {{count}}",
  chipMine: "{{emoji}} {{count}}, you reacted",
  reactedBy: "Reacted by {{names}}",
  reactions: "Reactions",
};

export type ThreadCopy = typeof threadEn;

export const threadVi: ThreadCopy = {
  reply: "Trả lời",
  replyTo: "Trả lời {{name}}",
  replyPlaceholder: "Viết câu trả lời…",
  replySend: "Gửi trả lời",
  replyCancel: "Huỷ",
  replies: "Trả lời cho {{name}}",
  showMore_one: "Hiện thêm {{count}} câu trả lời",
  showMore_other: "Hiện thêm {{count}} câu trả lời",
  addReaction: "Thêm cảm xúc",
  pick: "Thả {{emoji}}",
  chip: "{{emoji}} {{count}}",
  chipMine: "{{emoji}} {{count}}, bạn đã thả",
  reactedBy: "Đã thả bởi {{names}}",
  reactions: "Cảm xúc",
};
