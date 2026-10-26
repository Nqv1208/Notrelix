const OPENABLE = /^(https?:|mailto:|tel:)/i;

export interface DocumentLinkEnv {
  readonly workspaceId: string;
  readonly openUrl: (url: string) => Promise<unknown> | void;
  readonly push: (route: string) => void;
}

/** What a tap on a link or page reference does in the reader. */
export function createDocumentLinkHandlers(env: DocumentLinkEnv) {
  return {
    onOpenLink(href: string): void {
      if (!OPENABLE.test(href)) return;
      void env.openUrl(href);
    },
    onOpenPage(pageId: string): void {
      env.push(
        `/workspaces/${encodeURIComponent(env.workspaceId)}/docs/${encodeURIComponent(pageId)}`,
      );
    },
  };
}
