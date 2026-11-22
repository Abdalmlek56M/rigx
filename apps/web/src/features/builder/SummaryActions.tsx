"use client";

import { useLocale, useTranslations } from "next-intl";
import { Button, ButtonLink } from "@/components/ui/Button";
import { TextField } from "@/components/ui/TextField";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { CopyIcon, DocumentIcon, ShareIcon } from "@/components/ui/icons";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { formatEgp } from "@/lib/format";
import { useCart } from "@/features/cart/cart-context";
import { cartLinesOf, partsSummaryText, type Evaluation } from "./builder-summary";
import { SHARE_NAME_MAX, quotePath, type PartInput } from "./builder-state";
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
  /** The optional name of the build, shown on the shared page. */
  name: string;
  onNameChange: (value: string) => void;
}

/** Add everything to the cart, ask on WhatsApp, copy the link, the PDF quote, and the "this does not reserve parts" note. */
export function SummaryActions({ evaluation, parts, checking, slotNames, whatsApp, shared, fallbackLink, onCopyLink, name, onNameChange }: SummaryActionsProps) {
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
      {hasParts && !canOrder && !checking ? <span className="text-xs leading-6 text-orange-text">{t("cannotOrder")}</span> : null}
      <div className="grid grid-cols-[repeat(auto-fit,minmax(0,1fr))] gap-2">
        {whatsApp.number ? (
          <WhatsAppButton
            href={whatsAppUrl(whatsApp.number, message)}
            variant="outline"
            disabled={!hasParts}
          >
            {t("whatsApp")}
          </WhatsAppButton>
        ) : null}
        {canOrder ? (
          <ButtonLink href={quotePath(parts)} external variant="secondary" icon={DocumentIcon}>{t("quote")}</ButtonLink>
        ) : (
          <Button type="button" variant="secondary" icon={DocumentIcon} disabled>{t("quote")}</Button>
        )}
      </div>
      <div className="flex flex-col gap-2 border-t border-dashed border-line pt-3.5">
        <b className="flex items-center gap-2 text-sm"><span aria-hidden="true" className="text-orange-text"><ShareIcon size={16} /></span>{t("shareTitle")}</b>
        <TextField
          label={t("shareName")}
          name="share-name"
          value={name}
          onChange={onNameChange}
          maxLength={SHARE_NAME_MAX}
          placeholder={t("shareNamePlaceholder")}
        />
        <Button type="button" variant="secondary" icon={CopyIcon} disabled={!hasParts || checking} onClick={onCopyLink} className="w-full">
          {t("copyLink")}
        </Button>
        <span role="status" className="text-xs text-ink-muted">{shared ? t(shared.saved ? "copiedSaved" : "copiedPlain") : ""}</span>
      </div>
      <span className="text-xs leading-6 text-ink-muted">{t("note")}</span>
    </>
  );
}
