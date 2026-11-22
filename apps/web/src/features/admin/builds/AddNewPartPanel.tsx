"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { formatEgp } from "@/lib/format";
import { offeredPrices } from "../categories/category-rules";
import { SellerPicker } from "../purchases/SellerPicker";
import type { SellerSummary } from "../purchases/purchases-api";
import { SELLER_PAYMENT_METHODS, type SellerPaymentMethod } from "../purchases/purchase-rules";
import { usePermissions } from "../use-permissions";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { formMessage } from "../form-message";
import { FormMessage } from "../kit/PageHeader";
import { ModelInput } from "../purchases/ModelInput";
import { BrandSelect } from "../stock/BrandSelect";
import { CategorySelect } from "../stock/CategorySelect";
import { SpecFields } from "../stock/SpecFields";
import { PartnerFields } from "../stock/PartnerFields";
import { applySuggestion, editModel, financingPartners } from "../stock/stock-fields";
import { WarrantyField } from "../stock/WarrantyField";
import type { BuildLookups } from "./BuildParts";
import { addNewPart } from "./builds-api";
import { emptyNewPart, newPartOwed, newPartValid, toNewPartBody, type NewPartForm } from "./build-rules";
import { useBuildAction, type BuildActionProps } from "./use-build-action";
import { CloseIcon, PlusIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const checkbox = "flex min-h-11 items-center gap-2 text-sm font-bold";

const SHOWN_FIELDS = [
  "categoryId", "brandId", "specs", "modelName", "condition", "serialNumber", "acquisitionCost", "price", "withBoardExtra", "aloneExtra", "partnerId", "partnerPercentOverride",
  "warrantyLabel", "internalNote", "sellerId", "paidAmount"
];

/**
 * A part that is not in stock yet: the same facts as a purchase line plus what it cost; it is created as a new RX unit and joins this build.
 * Picking a model that was in stock before copies its category, brand and specs.
 */
export function AddNewPartPanel({ lookups, onClose, ...action }: BuildActionProps & { lookups: BuildLookups; onClose: () => void }) {
  const t = useTranslations("admin.builds.addNew");
  const lines = useTranslations("admin.purchases.lines");
  const pay = useTranslations("admin.purchases.sellerPayment");
  const locale = useLocale();
  const { has } = usePermissions();
  const canBuy = has("Purchasing.CreateOrder") && has("Purchasing.ReceiveGoods");
  const { build } = action;
  const { submit, run } = useBuildAction(action);
  const [form, setForm] = useState<NewPartForm>(emptyNewPart);
  const [seller, setSeller] = useState<SellerSummary | null>(null);
  const set = (patch: Partial<NewPartForm>) => setForm((current) => ({ ...current, ...patch }));
  const error = (field: string) => submit.errors.fields[field];
  const setSpecs = useCallback((specs: NewPartForm["specs"]) => setForm((current) => ({ ...current, specs })), []);
  const isUsed = form.condition === "Used";
  const prices = offeredPrices(lookups.tree, form.categoryId || null);

  async function add() {
    if (!newPartValid(form)) return;
    await run(async () => {
      const saved = await addNewPart(build.id, toNewPartBody(form, build.rowVersion));
      onClose();
      return saved;
    }, t("added"));
  }

  return (
    <form
      className="flex flex-col gap-3 rounded-control border border-line bg-paper p-3"
      onSubmit={(event) => {
        event.preventDefault();
        void add();
      }}
    >
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        <CategorySelect tree={lookups.tree} name="newPartCategory" value={form.categoryId} onChange={(categoryId) => set({ categoryId })} error={error("categoryId")} />
        <ModelInput
          name="newPartModel" value={form.modelName} categoryId={form.categoryId} error={error("modelName")}
          onType={(text) => setForm((current) => editModel(current, text))} onPick={(suggestion) => setForm((current) => applySuggestion(current, suggestion))}
        />
        <BrandSelect brands={lookups.brands} name="newPartBrand" value={form.brandId} onChange={(brandId) => set({ brandId })} error={error("brandId")} />
        <SelectField
          label={lines("condition")} name="newPartCondition" value={form.condition} onChange={(condition) => set({ condition, ...(condition === "New" ? { serialNumber: "" } : {}) })} error={error("condition")}
          options={[{ value: "Used", label: lines("used") }, { value: "New", label: lines("new") }]}
        />
        {isUsed ? <TextField label={lines("serial")} name="newPartSerial" ltr value={form.serialNumber} onChange={(serialNumber) => set({ serialNumber })} error={error("serialNumber")} /> : null}
        <TextField label={t("cost")} name="newPartCost" type="number" ltr value={form.acquisitionCost} onChange={(acquisitionCost) => set({ acquisitionCost })} error={error("acquisitionCost")} />
        <TextField label={lines("price")} name="newPartPrice" type="number" ltr value={form.price} onChange={(price) => set({ price })} error={error("price")} />
        {prices.withBoard ? <TextField label={lines("withBoardExtra")} name="newPartWithBoard" type="number" ltr value={form.withBoardExtra} onChange={(withBoardExtra) => set({ withBoardExtra })} error={error("withBoardExtra")} /> : null}
        {prices.alone ? <TextField label={lines("aloneExtra")} name="newPartAlone" type="number" ltr value={form.aloneExtra} onChange={(aloneExtra) => set({ aloneExtra })} error={error("aloneExtra")} /> : null}
        {lookups.partners ? (
          <PartnerFields
            partners={financingPartners(lookups.partners)} partnerId={form.partnerId} percent={form.partnerPercent} namePrefix="newPartPartner"
            onChange={(patch) => set({ ...(patch.partnerId !== undefined ? { partnerId: patch.partnerId } : {}), ...(patch.percent !== undefined ? { partnerPercent: patch.percent } : {}) })}
            errors={{ partnerId: error("partnerId"), percent: error("partnerPercentOverride") }}
          />
        ) : null}
        <WarrantyField name="newPartWarranty" value={form.warrantyLabel} onChange={(warrantyLabel) => set({ warrantyLabel })} options={lookups.warrantyLabels} error={error("warrantyLabel")} />
      </div>
      <SpecFields categoryId={form.categoryId} values={form.specs} onChange={setSpecs} namePrefix="newPartSpec" error={error("specs")} />
      <div className="flex flex-wrap gap-x-6">
        <label className={checkbox}>
          <input type="checkbox" checked={form.hasBox} onChange={(event) => set({ hasBox: event.target.checked })} className="h-5 w-5" />
          {lines("hasBox")}
        </label>
        <label className={checkbox}>
          <input type="checkbox" checked={form.isHidden} onChange={(event) => set({ isHidden: event.target.checked })} className="h-5 w-5" />
          {lines("isHidden")}
        </label>
      </div>
      <TextAreaField label={lines("internalNote")} name="newPartNote" value={form.internalNote} onChange={(internalNote) => set({ internalNote })} rows={2} maxLength={1000} error={error("internalNote")} />
      {canBuy ? (
        <div className="flex flex-col gap-3">
          <p className="m-0 text-xs text-ink-muted">{t("sellerHint")}</p>
          <SellerPicker
            seller={seller} readOnly={false} governorates={[]}
            onPick={(picked) => {
              setSeller(picked);
              set({ sellerId: picked?.id ?? "", ...(picked ? {} : { paidAmount: "" }) });
            }}
          />
          {seller ? (
            <div className="grid gap-3 sm:grid-cols-2">
              <SelectField
                label={t("method")} name="newPartPayMethod" value={form.paymentMethod} onChange={(value) => set({ paymentMethod: value as SellerPaymentMethod })}
                options={SELLER_PAYMENT_METHODS.map((value) => ({ value, label: pay(`methods.${value}`) }))}
              />
              <TextField label={t("paid")} name="newPartPaid" type="number" ltr value={form.paidAmount} onChange={(paidAmount) => set({ paidAmount })} error={error("paidAmount")} />
              <p className="m-0 text-sm font-bold sm:col-span-2">{t("owed", { amount: formatEgp(newPartOwed(form), locale) })}</p>
            </div>
          ) : null}
        </div>
      ) : null}
      <FormMessage message={formMessage(submit.errors, SHOWN_FIELDS)} />
      <div className="flex flex-wrap gap-2">
        <Button type="submit" variant="primary" icon={PlusIcon} loading={submit.busy} disabled={!newPartValid(form)}>{t("add")}</Button>
        <Button icon={CloseIcon} onClick={onClose}>{t("close")}</Button>
      </div>
      {submit.dialog}
    </form>
  );
}
