/** The words the reader draws itself (everything else is the author's text). */
export interface MobileDocLabels {
  readonly image: string;
  readonly file: string;
  readonly board: string;
  readonly page: string;
  readonly done: string;
  readonly notDone: string;
}

export const DEFAULT_DOC_LABELS: MobileDocLabels = {
  image: "Hình ảnh",
  file: "Tệp đính kèm",
  board: "Bảng công việc",
  page: "Trang liên kết",
  done: "Đã xong",
  notDone: "Chưa xong",
};
