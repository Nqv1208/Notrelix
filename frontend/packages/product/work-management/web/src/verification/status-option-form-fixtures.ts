/**
 * Deterministic fixtures for the status option form: the ERP board's Status
 * field names (board data, so Vietnamese in both locales) and one scenario per
 * story state.
 */
import type { StatusOptionFormProps } from "../components/status-option/status-option-form";
import {
  statusOptionFormCopy,
  type StatusOptionFormCopy,
} from "../components/status-option/status-option-form.copy";

export const STATUS_OPTION_TAKEN_NAMES: readonly string[] = [
  "Cần làm",
  "Đang làm",
  "Chờ duyệt",
  "Hoàn thành",
];

export type StatusOptionFormScenario = Omit<
  StatusOptionFormProps,
  "onSubmit" | "onCancel"
>;

export function statusOptionFormCopyFixture(
  locale: "vi" | "en" = "vi",
): StatusOptionFormCopy {
  return statusOptionFormCopy(locale);
}

/** A new status on the Status field: empty name, the default group and colour. */
export function statusOptionFormCreateScenario(
  locale: "vi" | "en" = "vi",
): StatusOptionFormScenario {
  return {
    copy: statusOptionFormCopy(locale),
    mode: "create",
    existingNames: STATUS_OPTION_TAKEN_NAMES,
  };
}

/** Changing "Đang làm": its name, colour and group are filled in. */
export function statusOptionFormEditScenario(
  locale: "vi" | "en" = "vi",
): StatusOptionFormScenario {
  return {
    copy: statusOptionFormCopy(locale),
    mode: "edit",
    initial: { name: "Đang làm", color: "#1E7BE0", category: "in_progress" },
    existingNames: STATUS_OPTION_TAKEN_NAMES.filter(
      (name) => name !== "Đang làm",
    ),
  };
}

/** The typed name is taken: the duplicate message shows, nothing can be sent. */
export function statusOptionFormDuplicateScenario(
  locale: "vi" | "en" = "vi",
): StatusOptionFormScenario {
  return {
    copy: statusOptionFormCopy(locale),
    mode: "create",
    initial: { name: "Chờ duyệt" },
    existingNames: STATUS_OPTION_TAKEN_NAMES,
  };
}

/** The write is in flight: the values stay and the button says "Đang lưu…". */
export function statusOptionFormPendingScenario(
  locale: "vi" | "en" = "vi",
): StatusOptionFormScenario {
  return {
    copy: statusOptionFormCopy(locale),
    mode: "create",
    initial: { name: "Chờ khách duyệt", color: "#C18A12", category: "done" },
    existingNames: STATUS_OPTION_TAKEN_NAMES,
    pending: true,
  };
}

/** A Select field other than Status has no group: name and colour only. */
export function statusOptionFormEdgeDataScenario(
  locale: "vi" | "en" = "vi",
): StatusOptionFormScenario {
  return {
    copy: statusOptionFormCopy(locale),
    mode: "create",
    initial: {
      name: "Chờ phản hồi từ bộ phận pháp chế và đối tác bên ngoài, ưu tiên xử lý trong tuần",
    },
    existingNames: [],
    showCategory: false,
  };
}
