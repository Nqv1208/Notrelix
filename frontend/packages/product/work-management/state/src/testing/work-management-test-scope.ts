import { createElement, useState, type ReactNode } from "react";
import { QueryClientProvider } from "@tanstack/react-query";
import { createQueryClient } from "@notrelix/query";
import {
  WorkManagementServicesProvider,
  type WorkManagementServices,
} from "../services";

/**
 * An isolated Work Management state scope for component tests and stories:
 * its own query client (the shared configuration, without retries, so a
 * failing fake service fails at once) and the given services. Hosts compose
 * their own query client and services instead (see the web host's app
 * providers).
 */
export function WorkManagementTestScope({
  services,
  children,
}: {
  services: WorkManagementServices;
  children: ReactNode;
}) {
  const [client] = useState(() => {
    const queryClient = createQueryClient();
    queryClient.setDefaultOptions({
      queries: { retry: false },
      mutations: { retry: false },
    });
    return queryClient;
  });
  return createElement(
    QueryClientProvider,
    { client },
    createElement(WorkManagementServicesProvider, { services, children }),
  );
}
