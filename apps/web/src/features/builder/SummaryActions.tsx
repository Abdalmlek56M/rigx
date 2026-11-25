"use client";

import { useLocale, useTranslations } from "next-intl";
import { Button, ButtonLink } from "@/components/ui/Button";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { AlertIcon, CartIcon, DocumentIcon, InfoIcon } from "@/components/ui/icons";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { formatEgp } from "@/lib/format";
import { useCart } from "@/features/cart/cart-context";
import { cartLinesOf, partsSummaryText, type Evaluation } from "./builder-summary";
import { quotePath, type PartInput } from "./builder-state";
import { ShareBox } from "./ShareBox";
import type { ShareLink } from "./share-link";
import type { Visibility } from "./build-visibility";

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
  /** The builder's own path with the parts, where sign-in comes back to. */
  builderPath: string;
  onCopyLink: () => void;
  /** The optional name of the build, shown on the shared page. */
  name: string;
  onNameChange: (value: string) => void;
  /** Who may open the shared link; only a signed-in visitor can pick anything but "by link". */
  visibility: Visibility;
  onVisibilityChange: (value: Visibility) => void;
  /** Saving a private or public build failed (it is never shared as an open link instead). */
  shareFailed: boolean;
}

/** Add everything to the cart, ask on WhatsApp, the PDF quote, the share box, and the "this does not reserve parts" note. */
export function SummaryActions({ evaluation, parts, checking, slotNames, whatsApp, shared, fallbackLink, builderPath, ...share }: SummaryActionsProps) {
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
      <div className="flex flex-col gap-2">
        <Button
          type="button"
          variant="accent"
          icon={CartIcon}
          disabled={!canOrder}
          onClick={() => {
            for (const line of cartLinesOf(evaluation)) add(line);
            open();
          }}
          className="w-full"
        >
          {t("addAll")}
        </Button>
        {hasParts && !canOrder && !checking ? (
          <p className="m-0 flex gap-1.5 text-xs leading-5 text-orange-text">
            <span aria-hidden="true" className="mt-0.5 flex shrink-0"><AlertIcon size={14} /></span>
            {t("cannotOrder")}
          </p>
        ) : null}
        <div className="grid grid-cols-[repeat(auto-fit,minmax(0,1fr))] gap-2">
          {whatsApp.number ? (
            <WhatsAppButton href={whatsAppUrl(whatsApp.number, message)} variant="outline" disabled={!hasParts}>
              {t("whatsApp")}
            </WhatsAppButton>
          ) : null}
          {canOrder ? (
            <ButtonLink href={quotePath(parts)} external variant="secondary" icon={DocumentIcon}>{t("quote")}</ButtonLink>
          ) : (
            <Button type="button" variant="secondary" icon={DocumentIcon} disabled>{t("quote")}</Button>
          )}
        </div>
      </div>
      <ShareBox canShare={hasParts && !checking} shared={shared} builderPath={builderPath} {...share} />
      <p className="m-0 flex gap-2 text-xs leading-5 text-ink-muted">
        <span aria-hidden="true" className="mt-0.5 flex shrink-0 text-orange-text"><InfoIcon size={14} /></span>
        {t("note")}
      </p>
    </>
  );
}
