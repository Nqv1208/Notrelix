import type { WorkspaceSummary } from "../../core/types/workspace";

export const avatarColors = [
  "bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300",
  "bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300",
  "bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300",
  "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300",
  "bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300",
  "bg-cyan-100 text-cyan-700 dark:bg-cyan-900/40 dark:text-cyan-300",
  "bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300",
];

export const workspaceColors = [
  "#6161ff",
  "#2a9d99",
  "#ff8940",
  "#8b5cf6",
  "#0f9f6e",
  "#dc3f6d",
] as const;

export function colorForWorkspace(id: string) {
  const hash = Array.from(id).reduce(
    (value, character) => value + character.charCodeAt(0),
    0,
  );
  return workspaceColors[hash % workspaceColors.length];
}

export function formatWorkspacePlan(workspace: WorkspaceSummary) {
  if (workspace.isPersonal) return "Personal";
  return workspace.plan.charAt(0).toUpperCase() + workspace.plan.slice(1);
}

export function getInitials(name: string) {
  return name
    .split(/\s+/)
    .map((part) => part[0])
    .join("")
    .toUpperCase()
    .slice(0, 2);
}

export function formatDateLabel(value: string): string {
  return value.slice(0, 10);
}