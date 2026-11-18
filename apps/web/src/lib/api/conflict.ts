import { ApiError } from "@rigx/api-client";

/** The save was refused because the row changed meanwhile (a fresh read should replace the form's rowVersion). */
export function isConflict(error: unknown): boolean {
  return error instanceof ApiError && error.status === 409;
}
