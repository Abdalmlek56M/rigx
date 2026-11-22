"use client";

import { useId } from "react";
import { useLocale, useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { formatEgp } from "@/lib/format";
import type { Brand } from "../brands/brands-api";
import type { Category } from "../categories/categories-api";
import type { Partner } from "../expenses/expenses-api";
import { CategorySelect } from "../stock/CategorySelect";
import { PartnerFields } from "../stock/PartnerFields";
import { BrandSelect } from "../stock/BrandSelect";
import { SpecFields } from "../stock/SpecFields";
import { WarrantyField } from "../stock/WarrantyField";
import { offeredPrices } from "../categories/category-rules";
import { ModelInput } from "./ModelInput";
import { dropPhotoCopy, lineDisplayName, lineMargin, lineTotal, pickModel, typeModel, type LineForm } from "./purchase-rules";
import { ChevronDownIcon, CloseIcon, CopyIcon, TrashIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const checkbox = "flex min-h-11 items-center gap-2 text-sm font-bold";

/** One purchased line, collapsible: its title, total and a red dot when something is still needed. */
export function PurchaseLineCard({ line, index, tree, brands, partners, warrantyLabels, errors, missingFields, submitted, open, serialRepeated, focusSerial, onToggle, onChange, onRemove, onDuplicate, onScanNext, onBrandCreated }: {
  line: LineForm;
  index: number;
  tree: readonly Category[];
  brands: readonly Brand[];
  /** Active financing partners, or `null` when the staff member cannot read them (the partner fields are then left out). */
  partners: readonly Partner[] | null;
  warrantyLabels: readonly string[] | null;
  errors: Record<string, string>;
  missingFields: readonly string[];
  submitted: boolean;
  open: boolean;
  /** The serial number is also typed on another line. */
  serialRepeated: boolean;
  /** The serial field takes the focus when the card appears (the line a scan just opened). */
  focusSerial: boolean;
  onToggle: () => void;
  onChange: (line: LineForm) => void;
  onRemove: (() => void) | null;
  onDuplicate: () => void;
  /** Enter in the serial field (a scanner's last key): the next line opens for the next scan. */
  onScanNext: () => void;
  onBrandCreated?: (brand: Brand) => void;
}) {
  const t = useTranslations("admin.purchases.lines");
  const common = useTranslations("admin.purchases");
  const locale = useLocale();
  const panelId = useId();
  const set = (patch: Partial<LineForm>) => onChange({ ...line, ...patch });
  const field = (name: string) => `line${index}-${name}`;
  const errorFor = (name: string) =>
    errors[`lines.${index}.${name}`] ?? (submitted && missingFields.includes(name) ? common("required") : undefined);
  const isUsed = line.condition === "Used";
  const title = lineDisplayName(line, tree, t("lineTitle", { number: index + 1 }));
  const missing = missingFields.length > 0;
  const prices = offeredPrices(tree, line.categoryId || null);
  const margin = lineMargin(line);

  return (
    <li className={`flex flex-col gap-3 rounded-control border bg-paper p-3 ${missing ? "border-error/40" : "border-line"}`}>
      <div className="flex flex-wrap items-start justify-between gap-2">
        <h3 className="m-0 flex min-w-0 flex-1 items-center">
          <button
            type="button" aria-expanded={open} aria-controls={panelId} onClick={onToggle}
            className="flex min-h-11 min-w-0 items-center gap-2 rounded-control text-start focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange"
          >
            <span aria-hidden="true" className={`shrink-0 transition-transform duration-200 motion-reduce:transition-none ${open ? "rotate-180" : ""}`}>
              <ChevronDownIcon size={18} />
            </span>
            <span dir="auto" className="truncate font-bold [unicode-bidi:isolate]">{title}</span>
            {missing ? <span className="h-2 w-2 shrink-0 rounded-full bg-error" aria-hidden="true" /> : null}
          </button>
        </h3>
        <span className="flex flex-wrap items-center gap-2 text-sm">
          <span className="text-ink-muted">{t("lineTotal")}</span>
          <span className="font-bold">{formatEgp(lineTotal(line), locale)}</span>
          <Button size="sm" variant="ghost" icon={CopyIcon} onClick={onDuplicate}>{t("duplicate")}</Button>
          {onRemove ? <Button size="sm" variant="danger" icon={TrashIcon} onClick={onRemove}>{t("remove")}</Button> : null}
        </span>
      </div>

      <div
        id={panelId} aria-hidden={!open} inert={!open}
        className={`grid transition-[grid-template-rows] duration-200 ease-out motion-reduce:transition-none ${open ? "grid-rows-[1fr]" : "grid-rows-[0fr]"}`}
      >
        <div className="min-h-0 overflow-hidden">
          <div className="flex flex-col gap-3 pt-3">
            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
              <CategorySelect tree={tree} name={field("categoryId")} value={line.categoryId} onChange={(categoryId) => set({ categoryId })} error={errorFor("categoryId")} />
              <ModelInput
                name={field("modelName")} value={line.modelName} categoryId={line.categoryId} error={errorFor("modelName")}
                onType={(text) => onChange(typeModel(line, text))} onPick={(suggestion) => onChange(pickModel(line, suggestion))}
              />
              <BrandSelect brands={brands} tree={tree} categoryId={line.categoryId} name={field("brandId")} value={line.brandId} onChange={(brandId) => set({ brandId })} error={errorFor("brandId")} onCreated={onBrandCreated} />
              <SelectField
                label={t("condition")} name={field("condition")} value={line.condition} onChange={(condition) => set({ condition, ...(condition === "New" ? { serialNumber: "" } : {}) })} error={errorFor("condition")}
                options={[{ value: "Used", label: t("used") }, { value: "New", label: t("new") }]}
              />
              {isUsed ? null : <TextField label={t("quantity")} name={field("quantity")} type="number" ltr value={line.quantity} onChange={(quantity) => set({ quantity })} error={errorFor("quantity")} />}
              {isUsed ? (
                <TextField
                  label={t("serial")} name={field("serialNumber")} ltr value={line.serialNumber} onChange={(serialNumber) => set({ serialNumber })}
                  error={errorFor("serialNumber") ?? (serialRepeated ? t("serialRepeated") : undefined)} onEnter={onScanNext} autoFocus={focusSerial}
                />
              ) : null}
              <TextField label={t("unitCost")} name={field("unitCost")} type="number" ltr value={line.unitCost} onChange={(unitCost) => set({ unitCost })} error={errorFor("unitCost")} />
              <TextField label={t("price")} name={field("price")} type="number" ltr value={line.price} onChange={(price) => set({ price })} error={errorFor("price")} />
              {margin ? (
                <p aria-live="polite" className="m-0 flex flex-wrap items-baseline gap-x-2 self-end rounded-control bg-surface px-3 py-2 text-sm sm:col-span-2 xl:col-span-1">
                  <span className="text-ink-muted">{t("margin")}</span>
                  <b dir="ltr" className={margin.amount < 0 ? "text-error-text" : ""}>{formatEgp(margin.amount, locale)}</b>
                  <span dir="ltr" className={`text-xs ${margin.amount < 0 ? "text-error-text" : "text-ink-muted"}`}>{t("marginPercent", { percent: margin.percent })}</span>
                </p>
              ) : null}
              {prices.withBoard ? <TextField label={t("withBoardExtra")} name={field("withBoardExtra")} type="number" ltr value={line.withBoardExtra} onChange={(withBoardExtra) => set({ withBoardExtra })} error={errorFor("withBoardExtra")} /> : null}
              {prices.alone ? <TextField label={t("aloneExtra")} name={field("aloneExtra")} type="number" ltr value={line.aloneExtra} onChange={(aloneExtra) => set({ aloneExtra })} error={errorFor("aloneExtra")} /> : null}
              {prices.withBoard || prices.alone ? <p className="m-0 self-start text-xs font-normal text-ink-muted sm:col-span-2 xl:col-span-3">{t("priceHint")}</p> : null}
              {partners && partners.length > 0 ? (
                <PartnerFields
                  partners={partners} partnerId={line.partnerId} percent={line.partnerPercent} namePrefix={`line${index}-partner`}
                  onChange={(patch) => set({ ...(patch.partnerId !== undefined ? { partnerId: patch.partnerId } : {}), ...(patch.percent !== undefined ? { partnerPercent: patch.percent } : {}) })}
                  errors={{ partnerId: errorFor("partnerId"), percent: errorFor("partnerPercent") }}
                />
              ) : null}
              <WarrantyField name={field("warrantyLabel")} value={line.warrantyLabel} onChange={(warrantyLabel) => set({ warrantyLabel })} options={warrantyLabels} error={errorFor("warrantyLabel")} />
            </div>
            {line.copyPhotosFromItemId ? (
              <p role="status" className="m-0 flex flex-wrap items-center gap-2 rounded-control bg-orange-soft px-3 py-2 text-sm">
                <CopyIcon size={16} />
                <span className="font-bold">
                  {line.copySource ? t("copied", { code: line.copySource.code, count: line.copySource.photoCount }) : t("copiedSaved")}
                </span>
                <Button size="sm" variant="ghost" icon={CloseIcon} onClick={() => onChange(dropPhotoCopy(line))}>{t("dropPhotos")}</Button>
              </p>
            ) : null}
            <SpecFields categoryId={line.categoryId} values={line.specs} onChange={(specs) => set({ specs })} namePrefix={field("spec")} error={errors[`lines.${index}.specs`]} />
            <div className="flex flex-wrap gap-x-6">
              <label className={checkbox}>
                <input type="checkbox" checked={line.hasBox} onChange={(event) => set({ hasBox: event.target.checked })} className="h-5 w-5" />
                {t("hasBox")}
              </label>
              <label className={checkbox}>
                <input type="checkbox" checked={line.isHidden} onChange={(event) => set({ isHidden: event.target.checked })} className="h-5 w-5" />
                {t("isHidden")}
              </label>
            </div>
            <TextAreaField label={t("internalNote")} name={field("internalNote")} value={line.internalNote} onChange={(internalNote) => set({ internalNote })} rows={2} maxLength={1000} error={errorFor("internalNote")} />
          </div>
        </div>
      </div>
    </li>
  );
}
