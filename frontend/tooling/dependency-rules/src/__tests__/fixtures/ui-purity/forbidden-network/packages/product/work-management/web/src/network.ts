export function loadRemoteData() {
  return fetch("/api/v1/workspaces");
}
