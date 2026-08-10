import React, { createContext, useContext, type ReactNode } from "react";
import type { MobileApplicationServices } from "../services/mobile-application-services";

const MobileApplicationServicesContext =
  createContext<MobileApplicationServices | null>(null);

export function MobileApplicationServicesProvider({
  services,
  children,
}: {
  services: MobileApplicationServices;
  children: ReactNode;
}) {
  return (
    <MobileApplicationServicesContext.Provider value={services}>
      {children}
    </MobileApplicationServicesContext.Provider>
  );
}

export function useMobileApplicationServices(): MobileApplicationServices {
  const context = useContext(MobileApplicationServicesContext);
  if (!context) {
    throw new Error(
      "useMobileApplicationServices must be used within a MobileApplicationServicesProvider",
    );
  }
  return context;
}
