export function VagueLoader(name: string) {
  return async () => await import(name);
}
