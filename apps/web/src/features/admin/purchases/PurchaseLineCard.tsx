"use client";

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
import { ModelInput } from "./ModelInput";
import { dropPhotoCopy, lineTotal, pickModel, typeModel, type LineForm } from "./purchase-rules";
import { CloseIcon, CopyIcon, TrashIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const checkbox = "flex min-h-11 items-center gap-2 text-sm font-bold";

/** One purchased line: what it is, its condition and quantity, cost and sale prices, financing partner and publishing choices. */
export function PurchaseLineCard({ line, index, tree, brands, partners, warrantyLabels, errors, onChange, onRemove }: {
  line: LineForm;
  index: number;
  tree: readonly Category[];
  brands: readonly Brand[];
  /** Active financing partners, or `null` when the staff member cannot read them (the partner fields are then left out). */
  partners: readonly Partner[] | null;
  warrantyLabels: readonly string[] | null;
  errors: Record<string, string>;
  onChange: (line: LineForm) => void;
  onRemove: (() => void) | null;
}) {
  const t = useTranslations("admin.purchases.lines");
  const locale = useLocale();
  const set = (patch: Partial<LineForm>) => onChange({ ...line, ...patch });
  const field = (name: string) => `line${index}-${name}`;
  const error = (name: string) => errors[`lines.${index}.${name}`];
  const isUsed = line.condition === "Used";

  return (
    <li className="flex flex-col gap-3 rounded-control border border-line bg-paper p-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="m-0 text-[15px] font-bold">{t("lineTitle", { number: index + 1 })}</h3>
        <span className="flex items-center gap-3 text-sm">
          <span className="text-ink-muted">{t("lineTotal")}</span>
          <span className="font-bold">{formatEgp(lineTotal(line), locale)}</span>
          {onRemove ? <Button size="sm" variant="danger" icon={TrashIcon} onClick={onRemove}>{t("remove")}</Button> : null}
        </span>
      </div>
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        <CategorySelect tree={tree} name={field("categoryId")} value={line.categoryId} onChange={(categoryId) => set({ categoryId })} error={error("categoryId")} />
        <ModelInput
          name={field("modelName")} value={line.modelName} categoryId={line.categoryId} error={error("modelName")}
          onType={(text) => onChange(typeModel(line, text))} onPick={(suggestion) => onChange(pickModel(line, suggestion))}
        />
        <BrandSelect brands={brands} name={field("brandId")} value={line.brandId} onChange={(brandId) => set({ brandId })} error={error("brandId")} />
        <SelectField
          label={t("condition")} name={field("condition")} value={line.condition} onChange={(condition) => set({ condition, ...(condition === "New" ? { serialNumber: "" } : {}) })} error={error("condition")}
          options={[{ value: "Used", label: t("used") }, { value: "New", label: t("new") }]}
        />
        {isUsed ? null : <TextField label={t("quantity")} name={field("quantity")} type="number" ltr value={line.quantity} onChange={(quantity) => set({ quantity })} error={error("quantity")} />}
        {isUsed ? <TextField label={t("serial")} name={field("serialNumber")} ltr value={line.serialNumber} onChange={(serialNumber) => set({ serialNumber })} error={error("serialNumber")} /> : null}
        <TextField label={t("unitCost")} name={field("unitCost")} type="number" ltr value={line.unitCost} onChange={(unitCost) => set({ unitCost })} error={error("unitCost")} />
        <TextField label={t("price")} name={field("price")} type="number" ltr value={line.price} onChange={(price) => set({ price })} error={error("price")} />
        <TextField label={t("withBoardExtra")} name={field("withBoardExtra")} type="number" ltr value={line.withBoardExtra} onChange={(withBoardExtra) => set({ withBoardExtra })} error={error("withBoardExtra")} />
        <TextField label={t("aloneExtra")} name={field("aloneExtra")} type="number" ltr value={line.aloneExtra} onChange={(aloneExtra) => set({ aloneExtra })} error={error("aloneExtra")} />
        {partners && partners.length > 0 ? (
          <PartnerFields
            partners={partners} partnerId={line.partnerId} percent={line.partnerPercent} namePrefix={`line${index}-partner`}
            onChange={(patch) => set({ ...(patch.partnerId !== undefined ? { partnerId: patch.partnerId } : {}), ...(patch.percent !== undefined ? { partnerPercent: patch.percent } : {}) })}
            errors={{ partnerId: error("partnerId"), percent: error("partnerPercent") }}
          />
        ) : null}
        <WarrantyField name={field("warrantyLabel")} value={line.warrantyLabel} onChange={(warrantyLabel) => set({ warrantyLabel })} options={warrantyLabels} error={error("warrantyLabel")} />
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
      <SpecFields categoryId={line.categoryId} values={line.specs} onChange={(specs) => set({ specs })} namePrefix={field("spec")} error={error("specs")} />
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
      <TextAreaField label={t("internalNote")} name={field("internalNote")} value={line.internalNote} onChange={(internalNote) => set({ internalNote })} rows={2} maxLength={1000} error={error("internalNote")} />
    </li>
  );
}
