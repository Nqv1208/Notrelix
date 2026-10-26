/**
 * The account phone number as the Identity producer keeps it: E.164, a leading
 * `+` and then 8 to 15 digits, the first not zero. A person may type spaces,
 * hyphens, dots or parentheses between the digits; they are dropped before the
 * check, exactly as the producer does. There is no default region: a number
 * typed in national form (`0912 345 678`) is refused with a hint to add the
 * country code instead of being guessed.
 */
export const PHONE_MIN_DIGITS = 8;
export const PHONE_MAX_DIGITS = 15;

export type PhoneCheck =
  /** Blank: the phone is cleared (or was never set). */
  | { readonly kind: "empty" }
  | { readonly kind: "valid"; readonly e164: string }
  | {
      readonly kind: "invalid";
      /** `country-code`: digits without a leading `+`; `format`: anything else. */
      readonly reason: "country-code" | "format";
    };

export function checkPhone(input: string): PhoneCheck {
  const stripped = input.replace(/[\s().-]/g, "");
  if (stripped === "") return { kind: "empty" };
  if (stripped[0] !== "+")
    return {
      kind: "invalid",
      reason: /^\d+$/.test(stripped) ? "country-code" : "format",
    };
  const digits = stripped.slice(1);
  if (
    !/^\d+$/.test(digits) ||
    digits.length < PHONE_MIN_DIGITS ||
    digits.length > PHONE_MAX_DIGITS ||
    digits[0] === "0"
  )
    return { kind: "invalid", reason: "format" };
  return { kind: "valid", e164: `+${digits}` };
}

/** The value a save sends: E.164, `""` to clear; `null` when the input is not a phone. */
export function phoneForSave(input: string): string | null {
  const check = checkPhone(input);
  if (check.kind === "valid") return check.e164;
  return check.kind === "empty" ? "" : null;
}
