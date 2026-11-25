"use client";

import { useTranslations } from "next-intl";
import { RefreshIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** The partner list did not load: say so and offer to read it again, never leave the field out silently. */
export function PartnersFailed({ onRetry, className = "" }: { onRetry: () => void; className?: string }) {
  const t = useTranslations("admin.inventory.partnerList");
  return (
    <div role="alert" className={`flex flex-wrap items-center justify-between gap-2 rounded-control border border-error/40 bg-paper px-3 py-2 ${className}`}>
      <p className="m-0 text-sm font-bold text-error-text">{t("failed")}</p>
      <Button size="sm" icon={RefreshIcon} onClick={onRetry}>{t("retry")}</Button>
    </div>
  );
}
