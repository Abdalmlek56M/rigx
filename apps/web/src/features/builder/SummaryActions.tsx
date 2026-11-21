"use client";

import { useLocale, useTranslations } from "next-intl";
import { Button, ButtonLink, IconButton } from "@/components/ui/Button";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { CopyIcon, DocumentIcon } from "@/components/ui/icons";
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
      <Button
        type="button"
        variant="accent"
        disabled={!canOrder}
        onClick={() => {
          for (const line of cartLinesOf(evaluation)) add(line);
          open();
        }}
        className="w-full"
      >
        {t("addAll")}
      </Button>
      {hasParts && !canOrder && !checking ? <span className="text-xs leading-6 text-[#FFC94D]">{t("cannotOrder")}</span> : null}
      {whatsApp.number ? (
        <WhatsAppButton
          href={whatsAppUrl(whatsApp.number, message)}
          variant="outline"
          className={`w-full ${hasParts ? "" : "pointer-events-none opacity-50"}`}
        >
          {t("whatsApp")}
        </WhatsAppButton>
      ) : null}
      <div className="grid grid-cols-[auto_1fr] gap-1.5">
        <IconButton
          icon={CopyIcon}
          label={t("copyLink")}
          variant="secondary"
          disabled={!hasParts || checking}
          onClick={onCopyLink}
        />
        {canOrder ? (
          <ButtonLink href={quotePath(parts)} external variant="secondary" icon={DocumentIcon}>{t("quote")}</ButtonLink>
        ) : (
          <Button type="button" variant="secondary" icon={DocumentIcon} disabled>{t("quote")}</Button>
        )}
      </div>
      <span role="status" className="text-xs text-footer-text">{shared ? t(shared.saved ? "copiedSaved" : "copiedPlain") : ""}</span>
      <span className="text-xs leading-6 text-footer-text">{t("note")}</span>
    </>
  );
}
