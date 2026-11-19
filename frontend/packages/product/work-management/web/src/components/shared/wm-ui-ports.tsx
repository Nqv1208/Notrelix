import { createContext, useContext, type ReactNode } from "react";

/** What the Relation picker reads of the board a Relation field points at. */
export interface RelationTargetView {
  readonly board?: { readonly title: string };
  readonly groups: readonly {
    readonly cards: readonly {
      readonly id: string;
      readonly itemKey?: string | null;
      readonly title: string;
    }[];
  }[];
  readonly isLoading: boolean;
  readonly error: unknown;
  readonly refetch: () => unknown;
}

/**
 * The data pure surfaces need that only the host can read (the pure UI rule forbids the state package):
 * each entry is a hook the host fills with its query. Without a host the surfaces show no image and an
 * empty target, the way the stories draw them.
 */
export interface WmUiPorts {
  /** The image of an image attachment of an item, as an object URL; null until read or when none. */
  readonly useCardImageUrl: (
    cardId: string,
    attachmentId: string | null | undefined,
  ) => string | null;
  /** The target board of a Relation field of the board in `workspaceId`. */
  readonly useRelationTarget: (
    boardId: string | undefined,
    workspaceId: string,
  ) => RelationTargetView;
}

const NO_TARGET: RelationTargetView = {
  groups: [],
  isLoading: false,
  error: null,
  refetch: () => undefined,
};

const WmUiPortsContext = createContext<WmUiPorts>({
  useCardImageUrl: () => null,
  useRelationTarget: () => NO_TARGET,
});

/** The host (see `WorkManagementUiPortsHost`) or a test supplies the ports. */
export function WmUiPortsProvider({
  ports,
  children,
}: {
  ports: WmUiPorts;
  children: ReactNode;
}) {
  return (
    <WmUiPortsContext.Provider value={ports}>
      {children}
    </WmUiPortsContext.Provider>
  );
}

export function useWmUiPorts(): WmUiPorts {
  return useContext(WmUiPortsContext);
}
