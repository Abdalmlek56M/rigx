"use client";

import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { PrinterIcon } from "@/components/ui/icons";

/** Prints the invoice (or saves it as a PDF) from the browser; hidden on the printed page. */
export function PrintButton() {
  const t = useTranslations("invoice");
  return <Button type="button" variant="primary" icon={PrinterIcon} onClick={() => window.print()}>{t("print")}</Button>;
}
