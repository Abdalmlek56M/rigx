import { useTranslations } from "next-intl";
import { StatusBadge } from "@/components/ui/StatusBadge";

/** The state of a staff account: active (green) or suspended (red), the same in the list and on the profile. */
export function UserStatusBadge({ isActive }: { isActive: boolean }) {
  const t = useTranslations("admin.users.status");
  return <StatusBadge tone={isActive ? "success" : "danger"}>{t(isActive ? "active" : "inactive")}</StatusBadge>;
}
