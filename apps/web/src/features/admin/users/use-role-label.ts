import { useTranslations } from "next-intl";
import type { StaffRole } from "./users-api";
import { roleLabel, type BuiltInRole } from "./user-rules.ts";

/** The built-in role labels from the messages, ready for `roleLabel` / `roleNames`. */
export function useBuiltInRoleLabel(): (role: BuiltInRole) => string {
  const t = useTranslations("admin.users.builtInRoles");
  return (role) => t(role);
}

/** A role's display name in the current language. */
export function useRoleLabel(): (role: Pick<StaffRole, "code" | "name">) => string {
  const builtIn = useBuiltInRoleLabel();
  return (role) => roleLabel(role, builtIn);
}
