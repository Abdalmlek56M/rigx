"use client";

import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { formatEgp } from "@/lib/format";
import { useCart } from "@/features/cart/cart-context";
import { cartLinesOf, partsSummaryText, type Evaluation } from "./builder-summary";
import { quotePath, type PartInput } from "./builder-state";
import type { ShareLink } from "./share-link";

interface SummaryActionsProps {
  evaluation: Evaluation;
  parts: readonly PartInput[];
  /** Waiting for the check of the latest change: nothing can be ordered or shared yet. */
  checking: boolean;
  slotNames: Readonly<Record<string, string>>;
  whatsApp: { number: string | null; template: string };
  /** The link copied for the current parts, if any; the WhatsApp message carries it, else the public `?parts=` URL. */
  shared: ShareLink | null;
  /** Public `?parts=` URL of the build ("" when the store has no domain set). */
  fallbackLink: string;
  onCopyLink: () => void;
}

const secondary = "flex h-11 items-center justify-center rounded-control border border-header-line bg-field px-1 text-[13px] text-on-carbon";

/** Add everything to the cart, ask on WhatsApp, copy the link, the PDF quote, and the "this does not reserve parts" note. */
export function SummaryActions({ evaluation, parts, checking, slotNames, whatsApp, shared, fallbackLink, onCopyLink }: SummaryActionsProps) {
  const t = useTranslations("builder.actions");
  const tc = useTranslations("common.condition");
  const locale = useLocale();
  const { add, open } = useCart();
  const hasParts = evaluation.parts.length > 0;
  const canOrder = evaluation.canOrder && !checking;
  const message = [
    fillTemplate(whatsApp.template, { item: t("whatsAppItem"), price: formatEgp(evaluation.total, locale), link: shared?.url ?? fallbackLink }) || t("whatsAppFallback"),
    partsSummaryText(evaluation.parts, slotNames, tc)
  ].join("\n");

  return (
    <>
      <button
        type="button"
        disabled={!canOrder}
        onClick={() => {
          for (const line of cartLinesOf(evaluation)) add(line);
          open();
        }}
        className="flex h-[52px] items-center justify-center rounded-control bg-orange font-bold text-on-orange disabled:opacity-50"
      >
        {t("addAll")}
      </button>
      {hasParts && !canOrder && !checking ? <span className="text-xs leading-6 text-[#FFC94D]">{t("cannotOrder")}</span> : null}
      {whatsApp.number ? (
        <a
          href={whatsAppUrl(whatsApp.number, message)}
          aria-disabled={!hasParts}
          className={`flex h-[50px] items-center justify-center rounded-control border-[1.5px] border-on-carbon font-bold ${hasParts ? "" : "pointer-events-none opacity-50"}`}
        >
          {t("whatsApp")}
        </a>
      ) : null}
      <div className="grid grid-cols-2 gap-1.5">
        <button
          type="button"
          disabled={!hasParts || checking}
          onClick={onCopyLink}
          className={`${secondary} disabled:opacity-50`}
        >
          {t("copyLink")}
        </button>
        {canOrder ? (
          <Link href={quotePath(parts)} target="_blank" className={secondary}>{t("quote")}</Link>
        ) : (
          <span aria-disabled="true" className={`${secondary} opacity-50`}>{t("quote")}</span>
        )}
      </div>
      <span role="status" className="text-xs text-footer-text">{shared ? t(shared.saved ? "copiedSaved" : "copiedPlain") : ""}</span>
      <span className="text-xs leading-6 text-footer-text">{t("note")}</span>
    </>
  );
}
