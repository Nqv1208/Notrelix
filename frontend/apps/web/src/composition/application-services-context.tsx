import React, { createContext, useContext } from 'react';
import type { WebApplicationServices } from './application-services';

const ApplicationServicesContext = createContext<WebApplicationServices | null>(null);

export interface ApplicationServicesProviderProps {
  readonly services: WebApplicationServices;
  readonly children: React.ReactNode;
}

export function ApplicationServicesProvider({ services, children }: ApplicationServicesProviderProps) {
  return (
    <ApplicationServicesContext.Provider value={services}>
      {children}
    </ApplicationServicesContext.Provider>
  );
}

export function useApplicationServices(): WebApplicationServices {
  const context = useContext(ApplicationServicesContext);
  if (!context) {
    throw new Error('useApplicationServices must be used within an ApplicationServicesProvider');
  }
  return context;
}
