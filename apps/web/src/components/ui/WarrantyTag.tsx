import { useTranslations } from "next-intl";
import { warrantyText } from "@/lib/warranty-text";
import { ShieldIcon } from "./icons";

/** "ضمان 3 شهور" after an orange shield: the one way a part's warranty is written beside it. Nothing when the part has no warranty label. */
export function WarrantyTag({ label, className = "" }: { label: string | null | undefined; className?: string }) {
  const t = useTranslations("common");
  const text = warrantyText(label, (value) => t("warranty", { label: value }));
  if (!text) return null;
  return (
    <span className={`inline-flex items-center gap-1 whitespace-nowrap text-xs font-semibold text-ink-muted ${className}`}>
      <span aria-hidden="true" className="inline-flex text-orange-text"><ShieldIcon size={13} /></span>
      {text}
    </span>
  );
}
