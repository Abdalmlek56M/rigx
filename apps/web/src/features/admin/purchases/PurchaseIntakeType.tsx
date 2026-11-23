"use client";

import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { cardClass } from "@/components/ui/Card";
import type { PurchaseForm } from "./purchase-rules";

const card = (selected: boolean) =>
  `flex cursor-pointer items-start gap-3 rounded-control border p-4 transition-colors duration-150 motion-reduce:transition-none ${
    selected ? "border-orange-text bg-orange-soft" : "border-line bg-paper"
  }`;

/** The first choice of a purchase: separate parts, or a whole PC kept as a build (with an opt-out to split it). */
export function PurchaseIntakeType({ form, error, onChange }: {
  form: PurchaseForm;
  error?: string | null;
  onChange: (patch: Partial<PurchaseForm>) => void;
}) {
  const t = useTranslations("admin.purchases.form");
  const fullPc = form.kind === "FullPc";

  return (
    <section className={cardClass({ className: "gap-3" })}>
      <h2 className="m-0 text-[17px] font-bold">{t("kindTitle")}</h2>
      <fieldset className="m-0 grid gap-3 border-0 p-0 sm:grid-cols-2">
        <legend className="sr-only">{t("kindTitle")}</legend>
        <label className={card(!fullPc)}>
          <input
            type="radio" name="intakeType" value="Parts" checked={!fullPc}
            onChange={() => onChange({ kind: "Parts", keepAsBuild: false, buildName: "" })}
            className="mt-1 h-4 w-4 shrink-0 accent-orange"
          />
          <span className="flex flex-col gap-1">
            <b>{t("partsTitle")}</b>
            <span className="text-xs font-normal text-ink-muted">{t("partsHint")}</span>
          </span>
        </label>
        <label className={card(fullPc)}>
          <input
            type="radio" name="intakeType" value="FullPc" checked={fullPc}
            onChange={() => onChange({ kind: "FullPc", keepAsBuild: true })}
            className="mt-1 h-4 w-4 shrink-0 accent-orange"
          />
          <span className="flex flex-col gap-1">
            <b>{t("buildTitle")}</b>
            <span className="text-xs font-normal text-ink-muted">{t("buildHint")}</span>
          </span>
        </label>
      </fieldset>

      {fullPc ? (
        <div className="grid gap-3 sm:grid-cols-2">
          <span className="hidden sm:block" aria-hidden="true" />
          <div className="flex flex-col gap-3 rounded-control border border-line bg-paper p-3">
            <label className="flex items-start gap-2 text-sm">
              <input
                type="checkbox" checked={!form.keepAsBuild}
                onChange={(event) => onChange({ keepAsBuild: !event.target.checked })}
                className="mt-0.5 h-4 w-4 shrink-0 accent-orange"
              />
              <span className="flex flex-col gap-0.5">
                <b>{t("splitTitle")}</b>
                <span className="text-xs font-normal text-ink-muted">{t("splitHint")}</span>
              </span>
            </label>
            {form.keepAsBuild ? (
              <TextField
                label={t("buildName")} name="buildName" value={form.buildName}
                onChange={(buildName) => onChange({ buildName })} error={error} maxLength={200}
              />
            ) : null}
          </div>
        </div>
      ) : null}
    </section>
  );
}
