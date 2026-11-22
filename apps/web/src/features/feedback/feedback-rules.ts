import { isEgyptMobile, isEmail } from "../requests/request-fields.ts";

/** The two things the feedback form sends; both are request types in the admin inbox. */
export const FEEDBACK_KINDS = ["Complaint", "Suggestion"] as const;
export type FeedbackKind = (typeof FEEDBACK_KINDS)[number];

export const MIN_MESSAGE = 10;
export const MAX_MESSAGE = 4000;
export const MAX_SUBJECT = 200;

export interface FeedbackDraft {
  name: string;
  phone: string;
  email: string;
  message: string;
}

export type FeedbackField = "name" | "phone" | "email" | "message";

/** The fields that cannot be sent as they are (the email is optional, but must be an address when given). */
export function feedbackProblems(draft: FeedbackDraft): FeedbackField[] {
  const problems: FeedbackField[] = [];
  if (draft.name.trim().length < 2) problems.push("name");
  if (!isEgyptMobile(draft.phone)) problems.push("phone");
  if (draft.email.trim() !== "" && !isEmail(draft.email)) problems.push("email");
  if (draft.message.trim().length < MIN_MESSAGE) problems.push("message");
  return problems;
}

/** A value kept for the API as `null` when empty. */
export function blankToNull(value: string): string | null {
  const text = value.trim();
  return text === "" ? null : text;
}
