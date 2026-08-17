/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL?: string;
  readonly VITE_WS_URL?: string;
  readonly VITE_APP_URL?: string;
  readonly VITE_RELEASE_SHA?: string;
  readonly VITE_MOCK_API?: string;
  readonly VITE_MOCK_PRESET?: string;
  readonly VITE_MOCK_PERSONA?: string;
  readonly VITE_MOCK_STATE?: string;
  readonly VITE_MOCK_DENSITY?: string;
  readonly VITE_MOCK_LATENCY?: string;
  readonly VITE_MOCK_SEED?: string;
}
