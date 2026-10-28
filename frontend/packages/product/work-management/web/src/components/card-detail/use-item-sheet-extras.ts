import { useState } from "react";
import {
  canRetryItemFileUpload,
  useCreateItemField,
  useDeleteCardFile,
  useItemChildItems,
  useItemFileUploads,
  useItemFollowing,
  usePeopleDirectory,
  useWorkManagementServices,
} from "@notrelix/work-management-state";
import {
  MAX_ITEM_FILE_BYTES,
  personInitials,
  type CardDetail,
  type CardFile,
} from "@notrelix/work-management-core";
import { formatFileSize } from "../item-page/item-page-format";
import { useItemPageCopy } from "../item-page/use-item-page-copy";
import { saveBlobAs } from "../item-page/save-blob";
import { fillSheetCopy } from "./item-sheet.copy";
import { useItemSheetCopy } from "./use-item-sheet-copy";
import type { TaskDetailCallbacks, TaskDetailData } from "./task-detail-types";

/**
 * What the item side sheet needs beyond the item itself, read and written the
 * way the item page does: files (upload with progress, download, delete),
 * following, sub-items and the people a comment can mention. A host spreads
 * `detailData` and `callbacks` into the sheet's own.
 */
export function useItemSheetExtras(card: CardDetail): {
  readonly detailData: Pick<
    TaskDetailData,
    | "childItems"
    | "following"
    | "followers"
    | "uploads"
    | "fileNotice"
    | "people"
  >;
  readonly callbacks: Pick<
    TaskDetailCallbacks,
    | "onToggleWatch"
    | "onToggleChildItem"
    | "onAddChildItem"
    | "onAddFile"
    | "onRetryUpload"
    | "onDismissUpload"
    | "onDownloadFile"
    | "onDeleteFile"
    | "onCreateField"
  >;
} {
  const copy = useItemPageCopy();
  const { cards } = useWorkManagementServices();
  const fileUploads = useItemFileUploads(card.id, card.workspaceId);
  const deleteFile = useDeleteCardFile(card.id, card.workspaceId);
  const follow = useItemFollowing(card.id, card.workspaceId);
  const children = useItemChildItems(card.id, card.workspaceId, card.boardId);
  const { directory } = usePeopleDirectory(card.boardId, card.workspaceId);
  const createField = useCreateItemField(card.boardId, card.workspaceId);
  const [fileNotice, setFileNotice] = useState<string | null>(null);
  const locale = useItemSheetCopy().locale;

  return {
    detailData: {
      childItems: children.data?.map((child) => ({
        ...child,
        itemKey: child.key,
      })),
      following: follow.following,
      followers: follow.followers.map((person) => ({
        userId: person.userId,
        name: person.name,
        initials: personInitials(person.name),
      })),
      uploads: fileUploads.uploads.map((upload) => ({
        ...upload,
        ...(upload.error === "tooBig"
          ? { limit: formatFileSize(MAX_ITEM_FILE_BYTES, locale) }
          : {}),
        retryable: canRetryItemFileUpload(upload),
      })),
      fileNotice,
      // Only people who can view the board are offered (decision 42); until the
      // directory is read the composer falls back to the item's own people.
      people: directory
        ? {
            people: directory.people,
            teams: directory.teams,
            canInvite: false,
          }
        : undefined,
    },
    callbacks: {
      onToggleWatch: (watched) => follow.setFollowing(watched),
      onToggleChildItem: (childId, done) => children.toggle({ childId, done }),
      onAddChildItem: (title) => children.addChild(title),
      onAddFile: fileUploads.start,
      onRetryUpload: fileUploads.retry,
      onDismissUpload: fileUploads.dismiss,
      onDownloadFile: (file: CardFile) => {
        setFileNotice(null);
        cards
          .downloadCardFile(card.id, file.id)
          .then((blob) => saveBlobAs(blob, file.name))
          .catch(() =>
            setFileNotice(
              fillSheetCopy(copy.files.downloadFailed, { name: file.name }),
            ),
          );
      },
      onDeleteFile: (file) =>
        deleteFile.mutateAsync(file.id).then(
          () => true,
          () => false,
        ),
      onCreateField: (input) =>
        createField(input).then(
          (id) => id,
          () => null,
        ),
    },
  };
}
