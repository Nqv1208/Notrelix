export const PROBLEM_KEYS = [
  "missedDeadlines",
  "disorganizedWorkflows",
  "unnecessaryComplexity",
  "slowProgress",
  "wastedTime",
  "lackOfCollaboration",
  "taskOverload",
] as const;

export type ProblemKey = (typeof PROBLEM_KEYS)[number];
