"use client";

import { useLocale, useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { WarrantyTag } from "@/components/ui/WarrantyTag";
import { CheckIcon, ImagePlaceholderIcon } from "@/components/ui/icons";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { describeIssue } from "./issue-text";
import type { Choice, OptionGroup } from "./option-groups";

interface OptionRowProps {
  group: OptionGroup;
  slotNames: Readonly<Record<string, string>>;
  specNames: Readonly<Record<string, string>>;
  onPick: (code: string) => void;
}

/**
 * One model as a card: the photo and the name on top, then one line per condition the store has (جديد, مستعمل), each with its condition pill,
 * its warranty and its price on the end edge, so prices line up down the list. A line is a button; the chosen one has an orange frame and a
 * filled check. A line that cannot be picked is flat grey and says why in place of the price.
 */
export function OptionRow({ group, slotNames, specNames, onPick }: OptionRowProps) {
  const t = useTranslations("builder.options");
  const ti = useTranslations("builder.issues");
  const locale = useLocale();
  const choices = [group.new, group.used].filter((choice): choice is Choice => choice !== null);
  const chosen = choices.some((choice) => choice.selected);

  function reason(choice: Choice): string | null {
    if (choice.block === "unavailable") return t("unavailable");
    if (choice.block === "full") return t("full");
    if (choice.block !== "incompatible") return null;
    const conflict = choice.issues.find((issue) => issue.level === "Conflict");
    if (!conflict) return t("incompatible");
    const text = describeIssue(conflict, slotNames, specNames);
    return ti(text.key, text.values);
  }

  return (
    <li className={`flex min-w-0 flex-col gap-3 rounded-card border bg-surface p-3.5 ${chosen ? "border-orange shadow-[0_0_0_3px_var(--orange-soft)]" : "border-line"}`}>
      <div className="flex min-w-0 items-center gap-3">
        <span className="flex size-14 shrink-0 items-center justify-center overflow-hidden rounded-control border border-line bg-paper text-muted">
          {group.thumbnailStorageKey ? (
            // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
            <img src={mediaUrl(group.thumbnailStorageKey)} alt="" width={56} height={56} loading="lazy" className="product-photo h-full w-full object-cover" />
          ) : (
            <ImagePlaceholderIcon size={28} />
          )}
        </span>
        <span dir="ltr" className="min-w-0 flex-1 text-start text-[15px] font-bold leading-snug [unicode-bidi:isolate]">{group.modelName}</span>
      </div>
      <div role="group" aria-label={t("conditionGroup", { model: group.modelName })} className="flex flex-col gap-2">
        {choices.map((choice) => {
          const why = reason(choice);
          const locked = choice.block !== null;
          return (
            <button
              key={choice.code}
              type="button"
              aria-pressed={choice.selected}
              disabled={locked}
              onClick={() => onPick(choice.code)}
              className={`press grid min-h-12 w-full grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-3 rounded-control border px-3 py-2 text-start transition-colors motion-reduce:transition-none ${
                choice.selected
                  ? "border-orange bg-orange-soft"
                  : locked
                    ? "cursor-not-allowed border-line bg-paper"
                    : "border-line bg-surface hover:border-orange/60"
              }`}
            >
              <span
                aria-hidden="true"
                className={`flex size-5 items-center justify-center rounded-full border-2 ${choice.selected ? "border-orange bg-orange text-on-orange" : locked ? "border-line" : "border-ink-muted/50"}`}
              >
                {choice.selected ? <CheckIcon size={12} /> : null}
              </span>
              <span className={`flex min-w-0 flex-wrap items-center gap-x-2.5 gap-y-1 ${locked ? "opacity-70" : ""}`}>
                <ConditionBadge condition={choice.condition} />
                <WarrantyTag label={choice.warrantyLabel} />
              </span>
              {why ? (
                <span className="max-w-[14rem] text-end text-xs font-semibold leading-5 text-ink-muted">{why}</span>
              ) : (
                <span className="flex flex-col items-end">
                  <b className="whitespace-nowrap text-[15px] tabular-nums">{formatEgp(choice.price, locale)}</b>
                  {choice.selected ? <span className="text-[11px] font-bold text-orange-text">{t("chosen")}</span> : null}
                </span>
              )}
            </button>
          );
        })}
      </div>
    </li>
  );
}
