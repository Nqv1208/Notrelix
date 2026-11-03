import { useSyncExternalStore } from "react";

/** The card the create popup has just made: the open card shows "Đã tạo ERP-153 trong Cần làm" for a while. */
interface CreatedNotice {
  readonly itemId: string;
  readonly groupTitle: string;
}

let notice: CreatedNotice | null = null;
const listeners = new Set<() => void>();
const emit = () => listeners.forEach((listener) => listener());

let timer: ReturnType<typeof setTimeout> | undefined;

/** The notice shows for a while (8 s, as the schema notices do), or until another card is created. */
export function announceCreated(next: CreatedNotice) {
  notice = next;
  clearTimeout(timer);
  timer = setTimeout(() => {
    notice = null;
    emit();
  }, 8000);
  emit();
}

export function useCreatedNotice(itemId: string): CreatedNotice | null {
  const current = useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => notice,
  );
  return current?.itemId === itemId ? current : null;
}
