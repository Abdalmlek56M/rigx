"use client";

import { useTranslations } from "next-intl";

export function PrintButton() {
  const t = useTranslations("invoice");
  return (
    <button type="button" onClick={() => window.print()} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon">
      {t("print")}
    </button>
  );
}
