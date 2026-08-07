import { AppError } from '@notrelix/kernel/src/errors/app-error';

// Violation: deep importing /src/
export const error = new AppError('test');
