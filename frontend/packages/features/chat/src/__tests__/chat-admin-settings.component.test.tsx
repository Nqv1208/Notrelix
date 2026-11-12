import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { createNotrelixClient } from "@notrelix/contracts";
import { I18nProvider } from "@notrelix/i18n";
import {
  MockStore,
  createMockFetch,
  defaultConfig,
  mockIds,
  type MockPersona,
} from "@notrelix/dev-mock-backend";
import { chatCatalog } from "../web/components/chat.catalog";
import { en, vi as viCopy } from "../web/components/chat.copy";
import {
  ExportSection,
  RetentionSection,
  type ExportSectionProps,
  type RetentionSectionProps,
} from "../web/components/chat-admin-settings";
import { ChatAdminSettings } from "../web/components/chat-admin-settings-host";
import { ChatSidebar } from "../web/components/chat-sidebar";
import { createChatHooks } from "../web/query/create-chat-hooks";
import type { ChatExport, ChatRetentionPolicy } from "../core/types/chat";

const WORKSPACE = mockIds.workspaces.primary;

beforeAll(() => {
  Element.prototype.scrollIntoView ??= () => undefined;
});

const policy = (
  over: Partial<ChatRetentionPolicy> = {},
): ChatRetentionPolicy => ({
  retentionDays: null,
  mode: "tombstone",
  version: 1,
  updatedAt: null,
  canChange: true,
  ...over,
});

function retention(over: Partial<RetentionSectionProps> = {}) {
  const onSave = vi.fn();
  render(
    <RetentionSection
      copy={en}
      policy={policy()}
      loading={false}
      loadFailed={false}
      busy={false}
      message={null}
      onRetry={() => undefined}
      onSave={onSave}
      {...over}
    />,
  );
  return { onSave };
}

describe("RetentionSection (UC-CHT-H05)", () => {
  it("lets the Owner set a tombstone window and sends it with the version", () => {
    const { onSave } = retention();
    fireEvent.change(screen.getByLabelText(en.admin.retention.daysLabel), {
      target: { value: "90" },
    });
    fireEvent.click(
      screen.getByRole("button", { name: en.admin.retention.save }),
    );
    expect(onSave).toHaveBeenCalledWith({
      retentionDays: 90,
      mode: "tombstone",
      expectedVersion: 1,
    });
  });

  it("refuses a window outside 7 to 3650 days and says why", () => {
    const { onSave } = retention();
    const input = screen.getByLabelText(en.admin.retention.daysLabel);
    fireEvent.change(input, { target: { value: "3" } });
    expect(input.getAttribute("aria-invalid")).toBe("true");
    expect(screen.getByText(en.admin.retention.daysInvalid)).toBeTruthy();
    const save = screen.getByRole("button", {
      name: en.admin.retention.save,
    }) as HTMLButtonElement;
    expect(save.disabled).toBe(true);
    expect(onSave).not.toHaveBeenCalled();
  });

  it("a purge asks first, states that it cannot be undone, and sends only after the confirmation", async () => {
    const { onSave } = retention();
    fireEvent.change(screen.getByLabelText(en.admin.retention.daysLabel), {
      target: { value: "30" },
    });
    fireEvent.click(
      screen.getByLabelText(new RegExp(en.admin.retention.purge)),
    );
    fireEvent.click(
      screen.getByRole("button", { name: en.admin.retention.save }),
    );

    const dialog = await screen.findByRole("alertdialog");
    expect(dialog.textContent).toContain("30 days");
    expect(dialog.textContent).toContain("cannot be restored");
    expect(onSave).not.toHaveBeenCalled();

    fireEvent.click(
      screen.getByRole("button", { name: en.admin.retention.confirm }),
    );
    expect(onSave).toHaveBeenCalledWith({
      retentionDays: 30,
      mode: "purge",
      expectedVersion: 1,
    });
  });

  it("cancelling the purge confirmation sends nothing", async () => {
    const { onSave } = retention();
    fireEvent.change(screen.getByLabelText(en.admin.retention.daysLabel), {
      target: { value: "30" },
    });
    fireEvent.click(
      screen.getByLabelText(new RegExp(en.admin.retention.purge)),
    );
    fireEvent.click(
      screen.getByRole("button", { name: en.admin.retention.save }),
    );
    await screen.findByRole("alertdialog");
    fireEvent.click(
      screen.getByRole("button", { name: en.admin.retention.cancel }),
    );
    expect(onSave).not.toHaveBeenCalled();
  });

  it("is read-only for anyone but the Owner: disabled fields, no save button, a note", () => {
    retention({ policy: policy({ retentionDays: 90, canChange: false }) });
    expect(
      screen.getByLabelText(en.admin.retention.daysLabel) as HTMLButtonElement,
    ).toHaveProperty("disabled", true);
    expect(screen.getByText(en.admin.retention.readOnly)).toBeTruthy();
    expect(
      screen.queryByRole("button", { name: en.admin.retention.save }),
    ).toBeNull();
  });

  it("shows the loading skeleton, and a failed read with a retry", () => {
    const onRetry = vi.fn();
    const { unmount } = render(
      <RetentionSection
        copy={en}
        policy={undefined}
        loading
        loadFailed={false}
        busy={false}
        message={null}
        onRetry={onRetry}
        onSave={() => undefined}
      />,
    );
    expect(
      screen.getByRole("status", { name: en.admin.retention.loading }),
    ).toBeTruthy();
    unmount();
    render(
      <RetentionSection
        copy={en}
        policy={undefined}
        loading={false}
        loadFailed
        busy={false}
        message={null}
        onRetry={onRetry}
        onSave={() => undefined}
      />,
    );
    fireEvent.click(
      screen.getByRole("button", { name: en.admin.retention.retry }),
    );
    expect(onRetry).toHaveBeenCalled();
  });

  it("has a Vietnamese copy for every retention and export key", () => {
    const keys = (value: unknown, prefix = ""): string[] =>
      value && typeof value === "object"
        ? Object.entries(value).flatMap(([k, v]) => keys(v, `${prefix}${k}.`))
        : [prefix];
    expect(keys(viCopy.admin).sort()).toEqual(keys(en.admin).sort());
  });
});

const job = (over: Partial<ChatExport> = {}): ChatExport => ({
  id: "e1",
  subjectUserId: "u1",
  rangeFrom: null,
  rangeTo: null,
  status: "ready",
  messageCount: 12,
  spaceCount: 3,
  excludedSpaceCount: 1,
  tombstoneCount: 2,
  sizeBytes: 4096,
  failureCode: null,
  createdAt: "2026-11-12T09:00:00Z",
  completedAt: "2026-11-12T09:01:00Z",
  expiresAt: "2026-11-13T09:01:00Z",
  downloadPath: "/x",
  ...over,
});

function exporter(over: Partial<ExportSectionProps> = {}) {
  const onRequest = vi.fn();
  const onDownload = vi.fn();
  render(
    <ExportSection
      copy={en}
      people={[
        { id: "u1", name: "An" },
        { id: "u2", name: "Binh" },
      ]}
      job={null}
      busy={false}
      downloading={false}
      error={null}
      formatTime={(iso) => `at ${iso}`}
      onRequest={onRequest}
      onDownload={onDownload}
      {...over}
    />,
  );
  return { onRequest, onDownload };
}

describe("ExportSection (UC-CHT-H06)", () => {
  it("needs a person, then sends the person and no range", () => {
    const { onRequest } = exporter();
    const button = screen.getByRole("button", {
      name: en.admin.export.request,
    });
    expect(button as HTMLButtonElement).toHaveProperty("disabled", true);
    fireEvent.change(screen.getByLabelText(en.admin.export.personLabel), {
      target: { value: "u2" },
    });
    fireEvent.click(button);
    expect(onRequest).toHaveBeenCalledWith({
      subjectUserId: "u2",
      rangeFrom: null,
      rangeTo: null,
    });
  });

  it("refuses an end that is not after the start", () => {
    exporter();
    fireEvent.change(screen.getByLabelText(en.admin.export.personLabel), {
      target: { value: "u1" },
    });
    fireEvent.change(screen.getByLabelText(en.admin.export.fromLabel), {
      target: { value: "2026-02-10" },
    });
    fireEvent.change(screen.getByLabelText(en.admin.export.toLabel), {
      target: { value: "2026-02-01" },
    });
    expect(screen.getByRole("alert").textContent).toContain(
      en.admin.export.rangeInvalid,
    );
    expect(
      (
        screen.getByRole("button", {
          name: en.admin.export.request,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });

  it("words a queued and a running export without a download", () => {
    exporter({ job: job({ status: "queued", downloadPath: null }) });
    expect(screen.getByText(en.admin.export.queued)).toBeTruthy();
    expect(
      screen.queryByRole("button", { name: en.admin.export.download }),
    ).toBeNull();
  });

  it("a ready export shows what it holds and leaves out, its expiry, and downloads", () => {
    const { onDownload } = exporter({ job: job() });
    expect(screen.getByRole("status").textContent).toContain(
      "12 messages in 3 spaces (4 KB)",
    );
    expect(screen.getByRole("status").textContent).toContain(
      "1 spaces you cannot see and 2 removed messages",
    );
    expect(screen.getByRole("status").textContent).toContain(
      "at 2026-11-13T09:01:00Z",
    );
    fireEvent.click(
      screen.getByRole("button", { name: en.admin.export.download }),
    );
    expect(onDownload).toHaveBeenCalledWith(
      expect.objectContaining({ id: "e1" }),
    );
  });

  it("an expired export asks for a new one", () => {
    exporter({ job: job({ status: "expired", downloadPath: null }) });
    expect(screen.getByText(en.admin.export.expired)).toBeTruthy();
  });

  it("names the failure code of a failed export", () => {
    exporter({
      job: job({
        status: "failed",
        failureCode: "export-too-large",
        downloadPath: null,
      }),
    });
    expect(
      screen.getByText(en.admin.export.failedReasons["export-too-large"]),
    ).toBeTruthy();
  });
});

function host(persona: MockPersona, viewerRole: "owner" | "admin" | "member") {
  const store = new MockStore({ ...defaultConfig, persona });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: (input, init) => createMockFetch(store)(input, init),
  });
  const hooks = createChatHooks({
    api: client.api,
    endpoints: client.endpoints,
  });
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  render(
    <I18nProvider catalogs={[chatCatalog]} initialLocale="en">
      <QueryClientProvider client={queryClient}>
        <ChatAdminSettings
          open
          onOpenChange={() => undefined}
          hooks={hooks}
          workspaceId={WORKSPACE}
          viewerRole={viewerRole}
          people={new Map([[mockIds.users.admin, "Admin Person"]])}
          locale="en"
          copy={en}
        />
      </QueryClientProvider>
    </I18nProvider>,
  );
  return { store };
}

describe("ChatAdminSettings against the mock (UC-CHT-H05, H06)", () => {
  it("the Owner saves a rule and the dialog says it was saved", async () => {
    const { store } = host("owner", "owner");
    const input = await screen.findByLabelText(en.admin.retention.daysLabel);
    fireEvent.change(input, { target: { value: "45" } });
    fireEvent.click(
      screen.getByRole("button", { name: en.admin.retention.save }),
    );
    expect(await screen.findByText(en.admin.retention.saved)).toBeTruthy();
    expect(store.chat.retentionPolicies.get(WORKSPACE)).toMatchObject({
      retentionDays: 45,
      mode: "tombstone",
    });
  });

  it("an Admin reads the rule without a save button but can request an export that finishes and downloads", async () => {
    host("admin", "admin");
    expect(await screen.findByText(en.admin.retention.readOnly)).toBeTruthy();
    expect(
      screen.queryByRole("button", { name: en.admin.retention.save }),
    ).toBeNull();
    fireEvent.change(screen.getByLabelText(en.admin.export.personLabel), {
      target: { value: mockIds.users.admin },
    });
    fireEvent.click(
      screen.getByRole("button", { name: en.admin.export.request }),
    );
    expect(await screen.findByText(en.admin.export.queued)).toBeTruthy();
    await waitFor(
      () =>
        expect(
          screen.getByRole("button", { name: en.admin.export.download }),
        ).toBeTruthy(),
      { timeout: 6000 },
    );
  });

  it("a Member sees nothing of it", () => {
    host("member", "member");
    expect(screen.queryByRole("dialog")).toBeNull();
  });
});

describe("the sidebar entry to the settings", () => {
  const sidebar = (onOpenSettings?: () => void) =>
    render(
      <ChatSidebar
        channels={[]}
        status="ready"
        activeChannelId={null}
        unreadBySpace={new Map()}
        browse={null}
        canCreate
        copy={en}
        onSelect={() => undefined}
        onCreate={() => undefined}
        onOpenSettings={onOpenSettings}
        onJoin={() => undefined}
        onRetry={() => undefined}
        joiningId={null}
      />,
    );

  it("is offered when the host gives the handler, and opens it", () => {
    const open = vi.fn();
    sidebar(open);
    fireEvent.click(screen.getByRole("button", { name: en.admin.open }));
    expect(open).toHaveBeenCalled();
  });

  it("is absent without the handler", () => {
    sidebar();
    expect(screen.queryByRole("button", { name: en.admin.open })).toBeNull();
  });
});
