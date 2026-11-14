import type { SharePersonView } from "./board-share-models";

/** Avatar and Space-mark colours shared by the dialog (1440) and the sheet (390). */
export const PERSON_TONE: Record<SharePersonView["tone"], string> = {
  blue: "bg-identity-blue text-identity-blue-foreground",
  coral: "bg-identity-coral text-identity-coral-foreground",
  gold: "bg-identity-gold text-identity-gold-foreground",
  sage: "bg-identity-sage text-identity-sage-foreground",
  ember: "bg-identity-ember text-identity-ember-foreground",
};

export const MARK_TONE: Record<string, string> = {
  blue: "bg-mark-blue text-mark-blue-foreground",
  gold: "bg-mark-gold text-mark-gold-foreground",
  coral: "bg-mark-coral text-mark-coral-foreground",
  sage: "bg-mark-sage text-mark-sage-foreground",
  crimson: "bg-mark-crimson text-mark-crimson-foreground",
  violet: "bg-mark-violet text-mark-violet-foreground",
};
