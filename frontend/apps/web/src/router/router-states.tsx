import { useRouter } from "@tanstack/react-router";
import { useLocale } from "@notrelix/i18n";
import {
  Button,
  ErrorState,
  LoadingState,
  NotFoundState,
} from "@notrelix/ui-web";
import { en, vi } from "./router-states.copy";

function useRouterStatesCopy() {
  const { locale } = useLocale();
  return locale === "vi" ? vi : en;
}

/** The workspace id of the current location, when it is inside a workspace. */
function useHomeHref(): string {
  const router = useRouter();
  const match = /^\/workspaces\/([^/]+)/.exec(router.state.location.pathname);
  return match ? `/workspaces/${match[1]}` : "/home";
}

export function RouterPending() {
  const copy = useRouterStatesCopy().loading;
  return <LoadingState title={copy.title} description={copy.description} />;
}

/** Route error screen: a plain-language message, never the raw exception text. */
export function RouterError({ reset }: { reset?: () => void }) {
  const copy = useRouterStatesCopy().error;
  const router = useRouter();
  const home = useHomeHref();
  return (
    <ErrorState
      title={copy.title}
      description={copy.description}
      action={
        <div className="flex items-center justify-center gap-2">
          <Button
            type="button"
            onClick={() => {
              reset?.();
              void router.invalidate();
            }}
          >
            {copy.retry}
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => void router.navigate({ href: home })}
          >
            {copy.home}
          </Button>
        </div>
      }
    />
  );
}

export function RouterNotFound() {
  const copy = useRouterStatesCopy().notFound;
  const router = useRouter();
  const home = useHomeHref();
  return (
    <NotFoundState
      title={copy.title}
      description={copy.description}
      action={
        <Button
          type="button"
          variant="outline"
          onClick={() => void router.navigate({ href: home })}
        >
          {copy.home}
        </Button>
      }
    />
  );
}
