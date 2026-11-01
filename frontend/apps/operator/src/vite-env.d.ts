/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base URL of the operator API, for example http://127.0.0.1:8000/operator/v1. Defaults to the same origin. */
  readonly VITE_OPERATOR_API_URL?: string;
}
interface ImportMeta {
  readonly env: ImportMetaEnv;
}
