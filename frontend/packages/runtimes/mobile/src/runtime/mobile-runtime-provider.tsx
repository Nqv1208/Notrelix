import React, { createContext, useContext, type ReactNode } from "react";
import type { MobileRuntime } from "./mobile-runtime";

const MobileRuntimeContext = createContext<MobileRuntime | null>(null);

export function MobileRuntimeProvider({
  runtime,
  children,
}: {
  runtime: MobileRuntime;
  children: ReactNode;
}) {
  return (
    <MobileRuntimeContext.Provider value={runtime}>
      {children}
    </MobileRuntimeContext.Provider>
  );
}

export function useMobileRuntime(): MobileRuntime {
  const context = useContext(MobileRuntimeContext);
  if (!context) {
    throw new Error(
      "useMobileRuntime must be used within a MobileRuntimeProvider",
    );
  }
  return context;
}
