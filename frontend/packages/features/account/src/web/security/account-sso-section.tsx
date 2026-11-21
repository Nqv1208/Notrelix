import * as React from "react";
import { useAccountId } from "@notrelix/query";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import type { AccountService } from "../../core/api/account.service";
import { ssoFailureOf, type SsoSettings } from "../../core/sso";
import { accountQueryKeys } from "../../query/keys";
import type { AccountSecurityCopy } from "./account-security.copy";
import {
  AccountSsoPanel,
  type AccountSsoActions,
  type AccountSsoState,
  type SsoResult,
} from "./account-sso-panel";

/**
 * Reads the Account's single sign-on setup and answers the panel's calls. A 403 on the
 * read (or on any write) turns the whole panel into the permission-denied state; the
 * answer of every write is folded into the cached setup, so the panel never shows a
 * row the server does not have.
 */
export function AccountSsoSection({
  copy,
  service,
}: {
  copy: AccountSecurityCopy;
  service: AccountService;
}) {
  const accountId = useAccountId();
  const queryClient = useQueryClient();
  const key = accountQueryKeys.sso(accountId);
  const [denied, setDenied] = React.useState(false);
  const query = useQuery({
    queryKey: key,
    queryFn: () => service.getSso(),
    retry: false,
  });

  const edit = (change: (current: SsoSettings) => SsoSettings) =>
    queryClient.setQueryData<SsoSettings>(key, (current) =>
      current ? change(current) : current,
    );

  const run = async <T,>(
    call: () => Promise<T>,
    apply: (value: T) => void,
  ): Promise<SsoResult<T>> => {
    try {
      const value = await call();
      apply(value);
      return { ok: true, value };
    } catch (error) {
      const failure = ssoFailureOf(error);
      if (failure === "forbidden") setDenied(true);
      return { ok: false, failure };
    }
  };

  const actions: AccountSsoActions = {
    saveProvider: (input) =>
      run(
        () => service.saveSsoProvider(input),
        // The secret is write-only: the cache keeps only the fact that one is stored.
        (saved) =>
          edit((current) => ({
            ...current,
            provider: {
              name: input.name,
              issuerUrl: input.issuerUrl,
              clientId: input.clientId,
              hasClientSecret:
                input.clientSecret !== undefined ||
                current.provider?.hasClientSecret === true,
              status: saved.status,
              validationError: saved.validationError,
              jitProvisioning: input.jitProvisioning,
            },
          })),
      ),
    removeProvider: () =>
      run(
        async () => {
          await service.removeSsoProvider();
          return null;
        },
        () => edit((current) => ({ ...current, provider: null })),
      ),
    addDomain: (domain) =>
      run(
        () => service.addSsoDomain(domain),
        (row) =>
          edit((current) => ({
            ...current,
            // Adding a domain the Account already has answers its existing row.
            domains: current.domains.some((entry) => entry.id === row.id)
              ? current.domains.map((entry) =>
                  entry.id === row.id ? row : entry,
                )
              : [...current.domains, row],
          })),
      ),
    verifyDomain: (id) =>
      run(
        () => service.verifySsoDomain(id),
        (row) =>
          edit((current) => ({
            ...current,
            domains: current.domains.map((entry) =>
              entry.id === id ? row : entry,
            ),
          })),
      ),
    removeDomain: (id) =>
      run(
        async () => {
          await service.removeSsoDomain(id);
          return null;
        },
        () =>
          edit((current) => ({
            ...current,
            domains: current.domains.filter((entry) => entry.id !== id),
          })),
      ),
    setRequired: (required) =>
      run(
        async () => {
          await service.setSsoRequired(required);
          return null;
        },
        () => edit((current) => ({ ...current, requireSso: required })),
      ),
    retry: () => {
      setDenied(false);
      void query.refetch();
    },
  };

  const state: AccountSsoState =
    denied || (query.data && !query.data.canManage)
      ? { status: "forbidden" }
      : query.data
        ? { status: "ready", data: query.data }
        : query.isError
          ? ssoFailureOf(query.error) === "forbidden"
            ? { status: "forbidden" }
            : { status: "error" }
          : { status: "loading" };

  return <AccountSsoPanel copy={copy} state={state} actions={actions} />;
}
