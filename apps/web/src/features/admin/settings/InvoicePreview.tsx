"use client";

import { useTranslations } from "next-intl";
import { ScaledPaper } from "@/components/document/ScaledPaper";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { INVOICE_WIDTH } from "@/features/invoice/invoice-layout";
import { InvoiceBackgroundMark, InvoiceHeader } from "@/features/invoice/InvoiceHeader";
import { sampleInvoiceNumber } from "./preview-rules";

/** A grey bar standing for a line of text on the sample invoice. */
function Bar({ width }: { width: string }) {
  return <span aria-hidden="true" className="block h-3 rounded-full bg-line" style={{ width }} />;
}

/**
 * The top of the invoice as the customer gets it, drawn from the settings as they are being edited: the store's name and the logo band, the
 * invoice number with the prefix being typed, the faint background name, the warranty QR note and the terms line. It scales the real
 * invoice header, so what is seen here is what the invoice has.
 */
export function InvoicePreview({ storeName, prefix, backgroundLogo, warrantyQr, termsLine }: {
  storeName: string;
  prefix: string;
  backgroundLogo: boolean;
  warrantyQr: boolean;
  termsLine: string;
}) {
  const t = useTranslations("invoice");
  const tp = useTranslations("admin.settings.invoicePreview");
  const name = storeName.trim() || "RIGX";

  return (
    <figure className="m-0 flex flex-col gap-2">
      <figcaption className="type-label font-bold">{tp("title")}</figcaption>
      <div role="img" aria-label={tp("alt")} className="overflow-hidden rounded-control border border-line">
        <ScaledPaper width={INVOICE_WIDTH}>
          <article className="doc-paper relative isolate overflow-hidden bg-surface text-ink">
            {backgroundLogo ? <InvoiceBackgroundMark storeName={name} /> : null}
            <InvoiceHeader storeName={name} title={t("title")} kindLabel={t("kind.Parts")} />
            <div className="flex flex-col gap-5 p-8">
              <InfoGrid columns={3} className="rounded-card border border-line p-4">
                <InfoField label={t("number")} ltr value={sampleInvoiceNumber(prefix, new Date().getFullYear())} />
                <span className="flex flex-col gap-2 pt-1"><Bar width="70%" /><Bar width="50%" /></span>
                <span className="flex flex-col gap-2 pt-1"><Bar width="60%" /><Bar width="80%" /></span>
              </InfoGrid>
              <span className="flex flex-col gap-3"><Bar width="100%" /><Bar width="92%" /><Bar width="85%" /></span>
              {warrantyQr ? (
                <div className="flex items-start gap-3">
                  <span aria-hidden="true" className="size-20 shrink-0 rounded-control border-2 border-dashed border-line" />
                  <div className="flex max-w-[260px] flex-col gap-1 text-xs text-ink-muted">
                    <b className="text-sm text-ink">{t("warranty")}</b>
                    <span>{t("qrNote")}</span>
                  </div>
                </div>
              ) : null}
              <footer className="flex flex-col gap-1 border-t border-line pt-3 text-xs text-ink-muted">
                {termsLine.trim() ? <span>{termsLine.trim()}</span> : null}
                <span>{t("thanks")}</span>
              </footer>
            </div>
          </article>
        </ScaledPaper>
      </div>
    </figure>
  );
}
