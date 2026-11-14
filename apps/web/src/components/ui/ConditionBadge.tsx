import { useTranslations } from "next-intl";

/** "جديد" (orange) or "مستعمل" (carbon) — the only two condition labels customers ever see. */
export function ConditionBadge({ condition }: { condition: "New" | "Used" }) {
  const t = useTranslations("common.condition");
  const colors = condition === "New" ? "bg-orange text-on-orange" : "bg-carbon text-on-carbon";
  return (
    <span className={`inline-block whitespace-nowrap rounded px-2 py-[3px] text-xs font-bold ${colors}`}>
      {t(condition)}
    </span>
  );
}
