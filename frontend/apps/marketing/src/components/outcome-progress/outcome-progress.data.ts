import { Briefcase, CheckSquare, Users, Zap } from "lucide-react";
import type { OutcomeProgressItemData } from "./outcome-progress.types";

export const OUTCOME_PROGRESS_ITEMS: OutcomeProgressItemData[] = [
  {
    id: "connected-teams",
    value: "Connected Teams",
    label: "Unified workspaces & real-time collaboration",
    icon: Users,
    evidenceStatus: "qualitative",
  },
  {
    id: "clear-ownership",
    value: "Clear Ownership",
    label: "Structured tasks, roles & transparent progress",
    icon: CheckSquare,
    evidenceStatus: "qualitative",
  },
  {
    id: "automated-handoffs",
    value: "Automated Handoffs",
    label: "Seamless task-to-doc & calendar workflows",
    icon: Briefcase,
    evidenceStatus: "qualitative",
  },
  {
    id: "integrated-flow",
    value: "Integrated Flow",
    label: "One platform for docs, boards & schedules",
    icon: Zap,
    evidenceStatus: "qualitative",
  },
];
