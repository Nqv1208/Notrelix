/**
 * The initials of a person for an avatar: the first letter of the last two
 * words of the name. A role in brackets, such as "(Admin)", is not part of the
 * name and is left out.
 */
export function initialsOf(name: string): string {
  const words = name
    .replace(/\([^)]*\)/g, " ")
    .trim()
    .split(/\s+/)
    .filter(Boolean);
  const letters = words.slice(-2).map((word) => Array.from(word)[0] ?? "");
  return letters.join("").toUpperCase() || "?";
}

/** The design tokens an avatar fill can take (the identity colours of the theme). */
const AVATAR_TONES = [
  "bg-identity-blue text-identity-blue-foreground",
  "bg-identity-coral text-identity-coral-foreground",
  "bg-identity-ember text-identity-ember-foreground",
  "bg-identity-gold text-identity-gold-foreground",
  "bg-identity-sage text-identity-sage-foreground",
] as const;

/**
 * A stable colour for a person's avatar, picked from the id so the same person
 * keeps the same colour in every view. The result is a Tailwind class pair.
 */
export function avatarToneOf(userId: string): string {
  let hash = 0;
  for (const char of userId) {
    hash = (hash * 31 + char.codePointAt(0)!) | 0;
  }
  return AVATAR_TONES[Math.abs(hash) % AVATAR_TONES.length]!;
}
