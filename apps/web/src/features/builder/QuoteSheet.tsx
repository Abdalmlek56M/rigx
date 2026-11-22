"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { DocumentFrame } from "@/components/document/DocumentFrame";
import { Button, ButtonLink } from "@/components/ui/Button";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { Logo } from "@/components/ui/Logo";
import { ArrowBackIcon, PrinterIcon } from "@/components/ui/icons";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { parseCondition } from "@/lib/condition";
import { formatDate, formatEgp } from "@/lib/format";
import { parseUsage } from "@/lib/usage";
import { builderPath, partsOf, type PartInput } from "./builder-state";

type Quote = Schemas["QuoteDocument"];
type Outcome =
  | { status: "ready"; document: Quote; parts: PartInput[]; slotByCode: ReadonlyMap<string, string> }
  | { status: "unavailable" }
  | { status: "failed" };

const CONFLICT = 409;
const DESIGN_WIDTH = 1080;
const DESIGN_HEIGHT = 1350;

/** Prices a build into a quote: from explicit parts or from a saved build (its name and usage carry over). */
async function loadQuote(parts: PartInput[], saved: string | null): Promise<Outcome> {
  try {
    const api = browserApi();
    const source = saved
      ? await required(unwrap(api.GET("/api/v1/builder/saved/{token}", { params: { path: { token: saved } } })))
      : null;
    const quoteParts = source ? partsOf(source.evaluation) : parts;
    const [document, evaluation] = await Promise.all([
      required(unwrap(api.POST("/api/v1/builder/quote", { body: { name: source?.name ?? null, usage: source?.usage ?? null, parts: quoteParts } }))),
      required(unwrap(api.POST("/api/v1/builder/check", { body: { parts: quoteParts } })))
    ]);
    return { status: "ready", document, parts: quoteParts, slotByCode: new Map(evaluation.parts.map((part) => [part.item.code, part.slot])) };
  } catch (error) {
    return error instanceof ApiError && (error.status === CONFLICT || error.status === 404) ? { status: "unavailable" } : { status: "failed" };
  }
}

interface QuoteSheetProps {
  parts: PartInput[];
  saved: string | null;
  slotNames: Readonly<Record<string, string>>;
  /** The store's domain, shown on the card ("" when not set). */
  domain: string;
}

/** The quote as an official store document (1080×1350 on light paper, scaled to fit): no invoice number and no customer data; copy it as a picture or print / save it as a PDF. */
export function QuoteSheet({ parts, saved, slotNames, domain }: QuoteSheetProps) {
  const t = useTranslations("builder.quote");
  const tu = useTranslations("usage");
  const locale = useLocale();
  const [outcome, setOutcome] = useState<Outcome | null>(null);

  useEffect(() => {
    let cancelled = false;
    loadQuote(parts, saved).then((result) => {
      if (!cancelled) setOutcome(result);
    });
    return () => {
      cancelled = true;
    };
  }, [parts, saved]);

  if (outcome === null) return <p role="status" className="px-4 py-16 text-center text-ink-muted">{t("loading")}</p>;
  if (outcome.status !== "ready") {
    return (
      <div role="alert" className="mx-auto flex max-w-[640px] flex-col items-center gap-3 px-4 py-16 text-center">
        <b className="text-lg">{t(outcome.status === "unavailable" ? "unavailable" : "failed")}</b>
        <ButtonLink href="/builder" variant="secondary" icon={ArrowBackIcon}>{t("backToBuilder")}</ButtonLink>
      </div>
    );
  }

  const { quote } = outcome.document;
  const usage = parseUsage(quote.usage);
  const count = quote.lines.reduce((sum, line) => sum + line.quantity, 0);
  const meta = usage ? t("metaWithUsage", { usage: tu(usage), count }) : t("meta", { count });
  const modelAlign = locale === "ar" ? "text-right" : "text-left";

  return (
    <div data-print-document className="mx-auto flex max-w-[1080px] flex-col gap-4 px-4 py-8 md:py-12 print:max-w-none print:p-0">
      <DocumentFrame
        width={DESIGN_WIDTH} fileKind="quote" fileLabel={quote.title ?? ""}
        leading={<ButtonLink href={builderPath(outcome.parts)} variant="secondary" icon={ArrowBackIcon}>{t("backToBuilder")}</ButtonLink>}
        actions={<Button type="button" variant="primary" icon={PrinterIcon} onClick={() => window.print()}>{t("print")}</Button>}
      >
        <article
         
          className="doc-paper flex flex-col overflow-hidden"
          style={{ width: DESIGN_WIDTH, height: DESIGN_HEIGHT, printColorAdjust: "exact" }}
        >
          <header className="flex items-center justify-between bg-carbon px-16 py-10 text-on-carbon">
            <Logo width={200} />
            <div className="flex flex-col items-end gap-1.5 text-end">
              <span className="text-[44px] leading-none font-bold">{t("document")}</span>
              <span className="text-xl opacity-80">{t("pricedOn", { date: formatDate(quote.pricedAtUtc, locale) })}</span>
            </div>
          </header>
          <div className="flex flex-1 flex-col gap-8 px-16 py-12">
            <div className="flex flex-col gap-2.5 border-b-2 border-ink pb-6">
              <span className="text-[22px] text-ink-muted">{t("label")}</span>
              <h1 dir="auto" className="m-0 text-[52px] leading-[1.2] font-bold [unicode-bidi:isolate]">{quote.title || t("label")}</h1>
              <span className="text-[22px] text-ink-muted">{meta}</span>
            </div>
            <div className="flex flex-col">
            <div className="grid grid-cols-[170px_1fr_120px_170px] gap-[18px] bg-paper px-3 py-3 text-lg font-bold [print-color-adjust:exact]">
              <span>{t("part")}</span>
              <span>{t("item")}</span>
              <span>{t("condition")}</span>
              <span className={locale === "ar" ? "text-left" : "text-right"}>{t("price")}</span>
            </div>
            <ul className="m-0 flex list-none flex-col p-0">
              {quote.lines.map((line) => {
                const condition = parseCondition(line.condition);
                const slot = line.code ? outcome.slotByCode.get(line.code) : undefined;
                return (
                  <li key={`${line.code}-${line.name}`} className="grid grid-cols-[170px_1fr_120px_170px] items-center gap-[18px] border-b border-line px-3 py-[18px]">
                    <span className="text-xl text-ink-muted">{slot ? (slotNames[slot] ?? slot) : ""}</span>
                    <span dir="ltr" className={`text-2xl font-bold [unicode-bidi:isolate] ${modelAlign}`}>
                      {line.name}{line.quantity > 1 ? ` ×${line.quantity}` : ""}
                    </span>
                    {condition ? <span className="justify-self-start"><ConditionBadge condition={condition} /></span> : <span />}
                    <span dir="ltr" className={`text-[22px] ${locale === "ar" ? "text-left" : "text-right"}`}>{formatEgp(line.lineTotal, locale)}</span>
                  </li>
                );
              })}
            </ul>
            </div>
            <div className="mt-auto flex items-end justify-between gap-8">
              <div className="flex flex-col gap-2 text-lg text-ink-muted">
                <span>{t("validity")}</span>
                <span>{t("cta")}</span>
                {domain ? <span dir="ltr" className="font-mono text-2xl text-ink">{domain}</span> : null}
              </div>
              <div className="flex w-[440px] shrink-0 items-center justify-between gap-4 rounded-control bg-carbon px-6 py-5 text-on-carbon [print-color-adjust:exact]">
                <span className="text-[22px]">{t("total")}</span>
                <span className="text-[44px] leading-none font-bold">{formatEgp(quote.total, locale)}</span>
              </div>
            </div>
          </div>
        </article>
      </DocumentFrame>
    </div>
  );
}
