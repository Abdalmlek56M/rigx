import type { FormErrors } from "./kit/form-errors";

/**
 * The text for a form's `FormMessage`: the general error, else the first field error the form has no input for.
 * The kit drops a field error with no input on screen when other field errors exist, so a form that renders only some fields
 * lists the ones it renders in `shown` and never loses the rest.
 */
export function formMessage(errors: FormErrors, shown: readonly string[] = []): string | null {
  return errors.general ?? Object.entries(errors.fields).find(([field]) => !shown.includes(field))?.[1] ?? null;
}
