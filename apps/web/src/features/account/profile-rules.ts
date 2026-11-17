import { isEgyptMobile, normalizePhone } from "../requests/request-fields.ts";
import { MAX_PHOTO_BYTES, PHOTO_TYPES } from "../requests/request-photos.ts";

export type ProfileField = "fullName" | "phone";

/** The profile fields that are invalid: a name is required, the mobile is optional but must be an Egyptian mobile when given. */
export function profileErrors(form: { fullName: string; phone: string }): ProfileField[] {
  const errors: ProfileField[] = [];
  if (!form.fullName.trim()) errors.push("fullName");
  if (form.phone.trim() && !isEgyptMobile(form.phone)) errors.push("phone");
  return errors;
}

export function toProfileRequest(form: { fullName: string; phone: string }): { fullName: string; phoneNumber: string | null } {
  const phone = normalizePhone(form.phone);
  return { fullName: form.fullName.trim(), phoneNumber: phone || null };
}

/** Why an avatar file is refused, or `null` when it is fine (images up to 12 MB). */
export function avatarProblem(file: { size: number; type: string }): "type" | "size" | null {
  if (!PHOTO_TYPES.includes(file.type)) return "type";
  return file.size > MAX_PHOTO_BYTES ? "size" : null;
}

/** First letters of the first two words, for the avatar when there is no picture. */
export function initials(name: string): string {
  const letters = name.trim().split(/\s+/).filter(Boolean).slice(0, 2).map((word) => Array.from(word)[0] ?? "");
  return letters.join("").toUpperCase() || "؟";
}
