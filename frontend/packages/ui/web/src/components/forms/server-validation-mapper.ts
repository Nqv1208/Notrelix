export interface ServerValidationIssue {
  field?: string;
  message: string;
}

export interface ServerValidationTarget {
  setError(
    field: string,
    error: { type: string; message: string }
  ): void;
}

export function mapServerValidationErrors(
  target: ServerValidationTarget,
  errors: readonly ServerValidationIssue[],
  fallbackField = 'root'
): void {
  for (const error of errors) {
    target.setError(error.field ?? fallbackField, {
      type: 'server',
      message: error.message,
    });
  }
}
