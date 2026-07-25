/**
 * @notrelix/feature-integrations — Integrations core types.
 *
 * Framework-neutral: no React, no DOM.
 */

export type {
  IntegrationProvider,
  ConnectionStatus,
  IntegrationConnection,
  Webhook,
} from './types/integrations';

export { integrationsQueryKeys } from './query/keys';

export {
  createUseConnections,
  createUseDisconnect,
  createUseWebhooks,
} from './query/hooks/use-integrations';

export {
  createIntegrationsService,
  type IntegrationsApiClient,
  type IntegrationsEndpoints,
} from './api/integrations.service';
