import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "@tanstack/react-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useCurrentUser } from "@notrelix/features-auth";
import { useLocale } from "@notrelix/i18n";
import { useAppRuntime } from "@notrelix/runtime-web";
import {
  DEFAULT_COLOR_CYCLE,
  OnboardingSurface,
  emptyDraft,
  newInviteRow,
  slugFromName,
  useOnboardingCopy,
  type AddressCheck,
  type BoardTemplateId,
  type OnboardingDraftData,
  type OnboardingStepId,
  type OnboardingSummary,
} from "@notrelix/features-workspace/web";
import { clearApplicationCache } from "@/composition/application-lifecycle";
import { AuthGuard } from "@/shell/guards/auth-guard";
import { useOnboardingApi } from "./use-onboarding-api";

const SLUG_SHAPE = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
const SLUG_DELAY_MS = 350;

/** What "Lưu và thoát" keeps beside the form. */
interface SavedData {
  draft: OnboardingDraftData;
  spaceIds: Record<string, string>;
  created: { workspace: boolean; departments: boolean };
  slugEdited: boolean;
}

function readSaved(
  data: Record<string, unknown> | undefined,
): SavedData | null {
  const draft = data?.draft as OnboardingDraftData | undefined;
  if (!draft?.workspace) return null;
  return {
    draft,
    spaceIds: (data?.spaceIds as Record<string, string>) ?? {},
    created: (data?.created as SavedData["created"]) ?? {
      workspace: false,
      departments: false,
    },
    slugEdited: data?.slugEdited === true,
  };
}

/** The company setup: four steps and a last page, at `/onboarding`. */
export function OnboardingPage() {
  return (
    <AuthGuard>
      <SetupFlow />
    </AuthGuard>
  );
}

function SetupFlow() {
  const copy = useOnboardingCopy();
  const { locale } = useLocale();
  const user = useCurrentUser();
  const api = useOnboardingApi();
  const { api: runtimeClient } = useAppRuntime();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const saved = useQuery({
    queryKey: ["onboarding", "draft"],
    queryFn: () => api.getDraft(),
    staleTime: Infinity,
    retry: false,
  });

  const [step, setStep] = useState<OnboardingStepId>("workspace");
  const [draft, setDraft] = useState<OnboardingDraftData>(() => emptyDraft());
  const [workspaceId, setWorkspaceId] = useState<string | null>(null);
  const [spaceIds, setSpaceIds] = useState<Record<string, string>>({});
  const [created, setCreated] = useState({
    workspace: false,
    departments: false,
  });
  const [slugEdited, setSlugEdited] = useState(false);
  const [address, setAddress] = useState<AddressCheck>({ kind: "idle" });
  const [summary, setSummary] = useState<OnboardingSummary | null>(null);
  const [invitesSent, setInvitesSent] = useState(0);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<
    "unreachable" | "emailNotConfirmed" | null
  >(null);
  const [today] = useState(() => new Date());

  // Resume what "Lưu và thoát" kept, once.
  const restored = useRef(false);
  useEffect(() => {
    if (restored.current || !saved.isSuccess) return;
    restored.current = true;
    const found = saved.data.draft;
    const data = readSaved(found?.data);
    if (!found || !data) return;
    setDraft(data.draft);
    setWorkspaceId(found.workspaceId);
    setSpaceIds(data.spaceIds);
    setCreated(data.created);
    setSlugEdited(data.slugEdited);
    setStep(found.step);
  }, [saved.isSuccess, saved.data]);

  // The address: checked a moment after the last keystroke.
  const slug = draft.workspace.slug;
  useEffect(() => {
    if (step !== "workspace" || created.workspace) return;
    if (!slug) {
      setAddress({ kind: "idle" });
      return;
    }
    if (slug.length < 3 || slug.length > 30 || !SLUG_SHAPE.test(slug)) {
      setAddress({ kind: "invalid" });
      return;
    }
    setAddress({ kind: "checking" });
    let stale = false;
    const timer = setTimeout(() => {
      api.slugCheck(slug).then(
        (result) => {
          if (stale) return;
          setAddress(
            result.available
              ? { kind: "free" }
              : result.reason === "invalid"
                ? { kind: "invalid" }
                : { kind: "taken", suggestion: result.suggestion },
          );
        },
        () => {
          if (!stale) setError("unreachable");
        },
      );
    }, SLUG_DELAY_MS);
    return () => {
      stale = true;
      clearTimeout(timer);
    };
  }, [api, slug, step, created.workspace]);

  const catalog = useQuery({
    queryKey: [
      "onboarding",
      "catalog",
      draft.workspace.size,
      draft.workspace.name,
    ],
    queryFn: () => api.catalog(draft.workspace.size, draft.workspace.name),
    enabled: step !== "workspace" || created.workspace,
    staleTime: 60_000,
    retry: false,
  });

  // The first visit to the departments step starts from the suggestions.
  useEffect(() => {
    if (step !== "departments" || !catalog.data) return;
    if (draft.departments.length > 0 || created.departments) return;
    setDraft((current) => ({
      ...current,
      departments: catalog.data.departments.map((entry, index) => ({
        key: entry.key,
        name: entry.name,
        color:
          DEFAULT_COLOR_CYCLE[index % DEFAULT_COLOR_CYCLE.length] ?? "blue",
      })),
    }));
  }, [step, catalog.data]);

  // The invitation step starts with empty rows to type into.
  useEffect(() => {
    if (step !== "invite" || draft.invites.length > 0) return;
    const first = draft.departments[0]?.key ?? null;
    setDraft((current) => ({
      ...current,
      invites: [newInviteRow(first), newInviteRow(first), newInviteRow(first)],
    }));
  }, [step]);

  const onDraft = (next: OnboardingDraftData) => {
    const nameChanged = next.workspace.name !== draft.workspace.name;
    const slugChanged = next.workspace.slug !== draft.workspace.slug;
    if (slugChanged && !nameChanged) setSlugEdited(true);
    setDraft(
      nameChanged && !slugEdited && !created.workspace
        ? {
            ...next,
            workspace: {
              ...next.workspace,
              slug: slugFromName(next.workspace.name),
            },
          }
        : next,
    );
  };

  const run = async (work: () => Promise<void>) => {
    setPending(true);
    setError(null);
    try {
      await work();
    } catch (failure) {
      // The backend answers 403 until the address is confirmed.
      setError(
        (failure as { status?: number }).status === 403
          ? "emailNotConfirmed"
          : "unreachable",
      );
    } finally {
      setPending(false);
    }
  };

  const departmentName = (key: string | null) =>
    draft.departments.find((entry) => entry.key === key)?.name ?? null;

  const finish = async (board: OnboardingSummary["board"], sent: number) => {
    await api.clearDraft();
    setSummary({
      companyName: draft.workspace.name.trim(),
      address: `${draft.workspace.slug}${copy.workspace.suffix}`,
      departments: draft.departments.map((entry) => ({
        name: entry.name,
        color: entry.color,
      })),
      invitesSent: sent,
      board,
    });
    await queryClient.invalidateQueries();
    setStep("done");
  };

  const onNext = () =>
    void run(async () => {
      if (step === "workspace") {
        if (!created.workspace) {
          try {
            const made = await api.createWorkspace({
              name: draft.workspace.name.trim(),
              slug: draft.workspace.slug,
              size: draft.workspace.size,
              locale: draft.workspace.locale,
            });
            setWorkspaceId(made.id);
            setCreated((current) => ({ ...current, workspace: true }));
          } catch (failure) {
            const status = (failure as { status?: number }).status;
            if (status === 409 || status === 422) {
              const check = await api.slugCheck(draft.workspace.slug);
              setAddress(
                check.reason === "invalid"
                  ? { kind: "invalid" }
                  : { kind: "taken", suggestion: check.suggestion },
              );
              return;
            }
            throw failure;
          }
        }
        setStep("departments");
      } else if (step === "departments") {
        if (!created.departments && workspaceId) {
          const made = await api.createDepartments(
            workspaceId,
            draft.departments.map((entry) => ({
              name: entry.name.trim(),
              color: entry.color,
            })),
          );
          const ids: Record<string, string> = {};
          draft.departments.forEach((entry, index) => {
            const id = made.departments[index]?.id;
            if (id) ids[entry.key] = id;
          });
          setSpaceIds(ids);
          setCreated((current) => ({ ...current, departments: true }));
        }
        setStep("invite");
      } else if (step === "invite") {
        const rows = draft.invites.filter((row) => row.email.trim());
        let sent = 0;
        if (rows.length > 0 && workspaceId) {
          const result = await api.sendInvites(
            workspaceId,
            rows.map((row) => ({
              email: row.email.trim(),
              spaceId: row.departmentKey
                ? (spaceIds[row.departmentKey] ?? null)
                : null,
              role: row.role,
            })),
          );
          sent = result.sent;
        }
        setInvitesSent(sent);
        setStep("board");
      } else if (step === "board" && workspaceId) {
        const made = await api.createBoard(workspaceId, {
          templateId: draft.board.templateId,
          name: draft.board.name.trim(),
          spaceId: draft.board.departmentKey
            ? (spaceIds[draft.board.departmentKey] ?? null)
            : null,
        });
        await finish(
          {
            name: draft.board.name.trim(),
            templateId: made.templateId as BoardTemplateId,
            departmentName: departmentName(draft.board.departmentKey),
          },
          invitesSent,
        );
      }
    });

  const onResendConfirmation = () =>
    void run(async () => {
      await runtimeClient.api.post("/auth/email-verification/request", {});
    });

  const onBack = () => {
    if (step === "departments") setStep("workspace");
    else if (step === "invite") setStep("departments");
    else if (step === "board") setStep("invite");
  };

  const onSkip = () =>
    void run(async () => {
      if (step === "departments") setStep("invite");
      else if (step === "invite") {
        setInvitesSent(0);
        setStep("board");
      } else if (step === "board") await finish(null, invitesSent);
    });

  const onExit = () =>
    void run(async () => {
      if (step === "done") return;
      await api.saveDraft({
        step,
        workspaceId,
        data: {
          draft,
          spaceIds,
          created,
          slugEdited,
        } satisfies SavedData as unknown as Record<string, unknown>,
      });
      await runtimeClient.api.post(runtimeClient.endpoints.auth.logout, {});
      clearApplicationCache(queryClient, { sessionEnded: true });
      await navigate({ to: "/sign-in", replace: true });
    });

  const ownerName = user?.name ?? user?.email ?? "";
  const surfaceProps = useMemo(
    () => ({
      copy,
      step,
      draft,
      email: user?.email ?? "",
      ownerName,
      address,
      catalog: catalog.data ?? null,
      summary,
      pending,
      error,
      created,
      today: new Intl.DateTimeFormat(locale === "vi" ? "vi-VN" : "en-GB", {
        weekday: "long",
        day: "numeric",
        month: "long",
      }).format(today),
    }),
    [
      copy,
      step,
      draft,
      user?.email,
      ownerName,
      address,
      catalog.data,
      summary,
      pending,
      error,
      created,
      locale,
      today,
    ],
  );

  if (saved.isPending) return null;
  return (
    <OnboardingSurface
      {...surfaceProps}
      onDraft={onDraft}
      onUseSuggestion={(suggestion) => {
        setSlugEdited(true);
        setDraft({
          ...draft,
          workspace: { ...draft.workspace, slug: suggestion },
        });
      }}
      onNext={onNext}
      onBack={onBack}
      onSkip={onSkip}
      onExit={onExit}
      onResendConfirmation={onResendConfirmation}
      onEnterHome={() =>
        void navigate({
          to: "/workspaces/$workspaceId",
          params: { workspaceId: workspaceId ?? "" },
        })
      }
    />
  );
}
