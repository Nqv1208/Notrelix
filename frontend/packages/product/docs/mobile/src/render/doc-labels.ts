/** The words the reader draws itself (everything else is the author's text). */
export interface MobileDocLabels {
  readonly image: string;
  readonly file: string;
  readonly board: string;
  readonly page: string;
  readonly done: string;
  readonly notDone: string;
  readonly loading: string;
  readonly loadError: string;
  readonly retry: string;
  readonly noAccess: string;
  readonly untitled: string;
}

export const DEFAULT_DOC_LABELS: MobileDocLabels = {
  image: "Hình ảnh",
  file: "Tệp đính kèm",
  board: "Bảng công việc",
  page: "Trang liên kết",
  done: "Đã xong",
  notDone: "Chưa xong",
  loading: "Đang tải trang",
  loadError: "Không tải được trang. Thử lại sau.",
  retry: "Thử lại",
  noAccess: "Bạn không có quyền xem trang này, hoặc trang không còn nữa.",
  untitled: "Trang chưa đặt tên",
};
