/**
 * Initials of a person as the design draws Vietnamese names: the given name,
 * that is the last two words ("Trần Minh Quân" is "MQ", "Đỗ Khánh Linh" is
 * "KL"); a "Thu" before the last word belongs to the family-given pair, so the
 * first and last word are used ("Lý Thu Trang" is "LT"). Two words use both
 * letters, one word its first two letters. A trailing "(Role)" is ignored.
 */
export function personInitials(name: string): string {
  const words = name
    .replace(/\s{0,16}\([^)]{0,64}\)\s{0,16}$/, "")
    .trim()
    .split(/\s+/)
    .filter(Boolean);
  if (words.length === 0) return "?";
  const plainLast = words[words.length - 1]!.normalize("NFD").replace(
    /[\u0300-\u036f]/g,
    "",
  );
  // A given name of two letters or fewer ("Hà") is drawn with both letters.
  if (words.length >= 2 && Array.from(plainLast).length <= 2)
    return plainLast.toUpperCase();
  if (words.length === 1)
    return Array.from(words[0]!).slice(0, 2).join("").toUpperCase();
  const beforeLast = words[words.length - 2]!.normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .toLowerCase();
  const picked =
    words.length >= 3 && beforeLast === "thu"
      ? [words[0]!, words[words.length - 1]!]
      : words.slice(-2);
  return picked
    .map((word) => Array.from(word)[0])
    .join("")
    .toUpperCase();
}
