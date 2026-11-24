"use client";

import { useTranslations } from "next-intl";

/** The store's own words on how to pay by transfer; plain text (React escapes it), line breaks kept. */
export function TransferInstructions({ text, titled = true }: { text: string; titled?: boolean }) {
  const t = useTranslations("checkout.payment");
  return (
    <div className="flex flex-col gap-1">
      {titled ? <b className="text-sm text-ink">{t("transferTitle")}</b> : null}
      <p dir="auto" className="m-0 whitespace-pre-line">{text}</p>
    </div>
  );
}
