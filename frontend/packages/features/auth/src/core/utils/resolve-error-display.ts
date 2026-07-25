type TranslationFunction = (key: string) => string;

export function resolveErrorDisplay(
  message: string | undefined,
  t: TranslationFunction,
): string {
  if (!message) return '';
  // If the message is an i18n key, translate it
  if (message.startsWith('auth.')) {
    return t(message);
  }
  return message;
}
