import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from "react";
import {
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Label,
  Textarea,
  cn,
} from "@notrelix/ui-web";
import { ApiFailure } from "./api";

export function messageOf(error: unknown): string {
  return error instanceof ApiFailure
    ? error.message
    : "Something went wrong. Try again.";
}

/** Loads on mount and whenever `key` changes; `reload` fetches again without a loading flash. */
export function useLoad<T>(load: () => Promise<T>, key: string) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  // The loader closes over the current inputs; `key` names them, so only a new key refetches.
  const loader = useRef(load);
  loader.current = load;

  const reload = useCallback(async () => {
    try {
      setData(await loader.current());
      setError(null);
    } catch (e) {
      setError(messageOf(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    setLoading(true);
    void reload();
  }, [reload, key]);

  return { data, error, loading, reload };
}

export function Page({
  title,
  lede,
  actions,
  children,
}: {
  title: string;
  lede?: string;
  actions?: ReactNode;
  children: ReactNode;
}) {
  return (
    <section className="flex flex-col gap-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="m-0 text-page-title text-foreground">{title}</h1>
          {lede ? (
            <p className="m-0 max-w-[640px] text-body text-muted-foreground">
              {lede}
            </p>
          ) : null}
        </div>
        {actions}
      </header>
      {children}
    </section>
  );
}

export function Panel({
  title,
  children,
  className,
}: {
  title: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <section
      aria-label={title}
      className={cn(
        "flex flex-col gap-3 rounded-lg border bg-card p-5 text-card-foreground",
        className,
      )}
    >
      <h2 className="m-0 text-section-heading">{title}</h2>
      {children}
    </section>
  );
}

export function ErrorNote({ message }: { message: string | null }) {
  if (!message) return null;
  return (
    <p role="alert" className="m-0 text-compact text-destructive">
      {message}
    </p>
  );
}

export function Loading() {
  return (
    <p role="status" className="m-0 text-compact text-muted-foreground">
      Loading
    </p>
  );
}

const STATUS_VARIANT: Record<
  string,
  "default" | "secondary" | "destructive" | "outline"
> = {
  Active: "secondary",
  Suspended: "destructive",
  Closed: "outline",
  Deprecated: "outline",
  Archived: "outline",
  Revoked: "outline",
  Canceled: "outline",
  Disabled: "outline",
  Expired: "outline",
};

/** A status is always a word, never a color alone. */
export function StatusBadge({ status }: { status: string }) {
  return <Badge variant={STATUS_VARIANT[status] ?? "outline"}>{status}</Badge>;
}

export function Field({
  label,
  htmlFor,
  children,
}: {
  label: string;
  htmlFor: string;
  children: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={htmlFor}>{label}</Label>
      {children}
    </div>
  );
}

/** Native select: same look as an input, keyboard and screen-reader behavior of the platform. */
export const SELECT_CLASS =
  "h-9 w-full rounded-md border border-input bg-transparent px-3 text-sm outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50";

export const REASON_MAX = 500;

/**
 * Every operator action asks for a reason (1 to 500 characters); it is recorded with the actor, the before and
 * after, and the time. The dialog stays open with the server's message when the action is refused.
 */
export function ReasonDialog({
  open,
  title,
  description,
  confirmLabel,
  destructive,
  onConfirm,
  onClose,
}: {
  open: boolean;
  title: string;
  description: ReactNode;
  confirmLabel: string;
  destructive?: boolean;
  onConfirm: (reason: string) => Promise<void>;
  onClose: () => void;
}) {
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setReason("");
      setError(null);
    }
  }, [open]);

  const submit = async () => {
    setBusy(true);
    setError(null);
    try {
      await onConfirm(reason.trim());
      onClose();
    } catch (e) {
      setError(messageOf(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(next) => (next ? undefined : onClose())}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription asChild>
            <div>{description}</div>
          </DialogDescription>
        </DialogHeader>
        <Field
          label="Reason (kept in the audit trail)"
          htmlFor="operator-reason"
        >
          <Textarea
            id="operator-reason"
            value={reason}
            maxLength={REASON_MAX}
            rows={3}
            onChange={(e) => setReason(e.target.value)}
          />
        </Field>
        <ErrorNote message={error} />
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={busy}>
            Cancel
          </Button>
          <Button
            variant={destructive ? "destructive" : "default"}
            disabled={busy || reason.trim().length === 0}
            onClick={() => void submit()}
          >
            {busy ? "Working" : confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export const dateTime = (value: string | null | undefined) =>
  value ? new Date(value).toLocaleString() : "";
