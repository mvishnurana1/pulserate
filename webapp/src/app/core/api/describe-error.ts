import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from './models';

/** Turns an API failure into one sentence for the UI, preferring the server's problem details. */
export function describeError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Something went wrong. Please try again.';
  }

  if (error.status === 0) {
    return 'Cannot reach the API. Check that it is running.';
  }

  const problem = error.error as ProblemDetails | null;
  if (problem && typeof problem === 'object') {
    if (problem.detail) {
      return problem.detail;
    }
    if (problem.errors) {
      return Object.values(problem.errors).flat().join(' ');
    }
    if (problem.title) {
      return problem.title;
    }
  }

  return `Request failed (${error.status}).`;
}
