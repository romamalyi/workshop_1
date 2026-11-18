import { HttpErrorResponse } from '@angular/common/http';

export function firstApiErrorMessage(err: unknown, fallback = 'An error occurred.'): string {
  const response = err as HttpErrorResponse;
  return response?.error?.errors?.[0]?.errorMessage ?? fallback;
}
