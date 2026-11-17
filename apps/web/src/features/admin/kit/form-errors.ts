import { ApiError, STEP_UP_HEADER } from "@rigx/api-client";

/** The server's field name for "this action needs the emailed step-up code". */
export const STEP_UP_FIELD = "stepUpCode";

export interface FormErrors {
  /** First message per field, keyed by the form's own field name (camelCase, e.g. `name`, `lines.0.code`). */
  fields: Record<string, string>;
  /** A message for the whole form (conflict, not found, server problem), or `null`. */
  general: string | null;
}

export const NO_ERRORS: FormErrors = { fields: {}, general: null };

/** `Lines[0].Code` / `lines[0].code` → `lines.0.code`: the server's keys in the form's naming. */
export function fieldKey(serverKey: string): string {
  return serverKey
    .replace(/^\$\.?/, "")
    .replace(/\[(\d+)\]/g, ".$1")
    .split(".")
    .filter(Boolean)
    .map((part) => part.charAt(0).toLowerCase() + part.slice(1))
    .join(".");
}

/**
 * What a failed save shows: ProblemDetails field errors next to their fields, everything else as one message
 * (`fallback` when the server sent no title). Not an `ApiError` (network down) → only the fallback.
 */
export function toFormErrors(error: unknown, fallback: string): FormErrors {
  if (!(error instanceof ApiError)) return { fields: {}, general: fallback };
  const fields: Record<string, string> = {};
  for (const [key, messages] of Object.entries(error.fieldErrors)) {
    if (key === STEP_UP_FIELD || !messages[0]) continue;
    fields[fieldKey(key)] ??= messages[0];
  }
  const hasFields = Object.keys(fields).length > 0;
  const general = hasFields ? null : error.status >= 500 ? fallback : error.problem.detail || error.problem.title || fallback;
  return { fields, general };
}

/** True when the server refused because the action needs a fresh step-up code (ask for it, do not show an error). */
export function isStepUpRequired(error: unknown): boolean {
  return error instanceof ApiError && error.status === 403 && STEP_UP_FIELD in error.fieldErrors;
}

/** Request headers that carry the step-up code. */
export function stepUpHeaders(code: string): Record<string, string> {
  return { [STEP_UP_HEADER]: code };
}
