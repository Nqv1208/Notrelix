import { AppError } from '@notrelix/kernel';

type FormFieldPath<T extends Record<string, unknown>> = Extract<keyof T, string> | 'root';

type FormSetError<T extends Record<string, unknown>> = (
  field: FormFieldPath<T>,
  error: { type: string; message: string },
) => void;

export function applyServerValidationErrors<T extends Record<string, unknown>>(
  form: { setError: FormSetError<T> },
  error: unknown,
) {
  if (!(error instanceof AppError) || error.kind !== 'validation') {
    return;
  }

  const validationErrors = error.validationErrors;
  if (!validationErrors || typeof validationErrors !== 'object') {
    form.setError('root', {
      type: 'server',
      message: error.message || 'Validation failed.',
    });
    return;
  }

  try {
    Object.entries(validationErrors).forEach(([field, messages]) => {
      const message = Array.isArray(messages) ? messages[0] : String(messages);
      form.setError(field as FormFieldPath<T>, {
        type: 'server',
        message: message || 'Invalid value',
      });
    });
  } catch {
    form.setError('root', {
      type: 'server',
      message: error.message || 'Validation failed.',
    });
  }
}
