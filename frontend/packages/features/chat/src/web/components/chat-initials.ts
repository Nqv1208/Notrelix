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
