import type { LucideIcon } from "lucide-react";

export type EvidenceStatus = "verified" | "qualitative";

export type OutcomeProgressItemData = {
  id: string;
  value: string;
  label: string;
  icon: LucideIcon;
  evidenceStatus: EvidenceStatus;
};
