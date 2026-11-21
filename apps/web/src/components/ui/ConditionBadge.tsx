import { useTranslations } from "next-intl";
import { StatusBadge } from "./StatusBadge";
import { conditionTone } from "./status-badge-rules";

/** "جديد" (orange) or "مستعمل" (graphite): the only two condition labels customers ever see. */
export function ConditionBadge({ condition }: { condition: "New" | "Used" }) {
  const t = useTranslations("common.condition");
  return <StatusBadge tone={conditionTone(condition)}>{t(condition)}</StatusBadge>;
}
