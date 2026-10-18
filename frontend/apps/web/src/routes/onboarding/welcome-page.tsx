import { useEffect, useState } from "react";
import { useNavigate, useParams } from "@tanstack/react-router";
import { useMutation, useQuery } from "@tanstack/react-query";
import { useCurrentUser } from "@notrelix/features-auth";
import {
  OnboardingJoinSurface,
  departmentInitials,
  useOnboardingCopy,
  type JoinSpace,
} from "@notrelix/features-workspace/web";
import { THEME_ACCENTS, useTheme, type ThemeAccent } from "@notrelix/ui-web";
import { AuthGuard } from "@/shell/guards/auth-guard";
import { useOnboardingApi } from "./use-onboarding-api";

/** "Lý Thu Trang" → "LT". */
function initialsOf(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return "?";
  if (words.length === 1) return (words[0] ?? "").slice(0, 2).toUpperCase();
  return `${words[0]?.charAt(0) ?? ""}${words.at(-1)?.charAt(0) ?? ""}`.toUpperCase();
}

const asAccent = (value: string): ThemeAccent =>
  (THEME_ACCENTS as readonly string[]).includes(value)
    ? (value as ThemeAccent)
    : "blue";

/** What an invited person sees on the first visit to a workspace: `/welcome/{workspaceId}`. */
export function WelcomePage() {
  return (
    <AuthGuard>
      <Welcome />
    </AuthGuard>
  );
}

function Welcome() {
  const { workspaceId } = useParams({ from: "/welcome/$workspaceId" });
  const copy = useOnboardingCopy();
  const api = useOnboardingApi();
  const navigate = useNavigate();
  const user = useCurrentUser();
  const theme = useTheme();
  const join = useQuery({
    queryKey: ["onboarding", "join", workspaceId],
    queryFn: () => api.getJoin(workspaceId),
    retry: false,
    staleTime: Infinity,
  });
  const [displayName, setDisplayName] = useState<string | null>(null);
  const [notifyEmail, setNotifyEmail] = useState<"digest" | "mentions" | null>(
    null,
  );
  const [nameError, setNameError] = useState(false);

  useEffect(() => setNameError(false), [displayName]);

  const goToWorkspace = () =>
    void navigate({
      to: "/workspaces/$workspaceId",
      params: { workspaceId },
      replace: true,
    });

  const save = useMutation({
    mutationFn: (input: {
      displayName: string;
      notifyEmail: "digest" | "mentions";
    }) => api.saveJoin(workspaceId, input),
    onSuccess: goToWorkspace,
  });

  if (join.isPending) return null;
  if (join.isError || !join.data) {
    goToWorkspace();
    return null;
  }
  const data = join.data;
  const name = displayName ?? data.profile.displayName;
  const notify = notifyEmail ?? data.profile.notifyEmail;
  const spaces: JoinSpace[] = data.spaces.map((space) => ({
    id: space.id,
    name: space.name,
    color: space.color,
    open: space.open,
    member: !space.open,
    boards: space.boards,
  }));
  return (
    <OnboardingJoinSurface
      copy={copy}
      email={user?.email ?? ""}
      org={{
        name: data.workspace.name,
        initials: departmentInitials(data.workspace.name),
      }}
      inviterName={data.inviterName}
      spaces={spaces}
      displayName={name}
      initials={initialsOf(name)}
      mode={theme.mode}
      accent={theme.accent}
      companyAccent={asAccent(data.companyAccent)}
      notifyEmail={notify}
      pending={save.isPending}
      error={save.isError ? "unreachable" : nameError ? "name" : null}
      onDisplayName={setDisplayName}
      onMode={theme.setMode}
      onAccent={theme.setAccent}
      onNotifyEmail={setNotifyEmail}
      onStart={() => {
        if (!name.trim()) {
          setNameError(true);
          return;
        }
        save.mutate({ displayName: name.trim(), notifyEmail: notify });
      }}
      onLater={goToWorkspace}
    />
  );
}
