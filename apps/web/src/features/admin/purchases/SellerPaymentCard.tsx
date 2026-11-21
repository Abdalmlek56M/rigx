"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { WalletIcon } from "@/components/ui/icons";
import { isConflict } from "@/lib/api/conflict";
import { formatEgp } from "@/lib/format";
import { Button } from "../kit/Button";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { SectionCard } from "../kit/SectionCard";
import { useSubmit } from "../kit/use-submit";
import { paymentAmountValid } from "../orders/order-rules";
import { usePermissions } from "../use-permissions";
import { paySeller, type Purchase } from "./purchases-api";
import { SELLER_PAYMENT_METHODS, type SellerPaymentMethod } from "./purchase-rules";

/** What is still owed to the seller of a received purchase, and a dialog to pay it (the money leaves the chosen kind of account). */
export function SellerPaymentCard({ purchase, onUpdated, onStale }: { purchase: Purchase; onUpdated: (purchase: Purchase) => void; onStale: () => void }) {
  const t = useTranslations("admin.purchases.sellerPayment");
  const locale = useLocale();
  const { has } = usePermissions();
  const submit = useSubmit();
  const [open, setOpen] = useState(false);
  const [method, setMethod] = useState<SellerPaymentMethod>("Cash");
  const [amount, setAmount] = useState(String(purchase.remaining));
  if (purchase.status !== "Received" || !has("Purchasing.ReceiveGoods")) return null;
  const valid = paymentAmountValid(amount, purchase.remaining);

  async function confirm() {
    await submit.run(async () => {
      try {
        onUpdated(await paySeller(purchase.id, Number(amount), method, purchase.rowVersion));
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
      setOpen(false);
    }, t("done"));
  }

  return (
    <SectionCard
      title={t("title")} icon={WalletIcon}
      actions={purchase.remaining > 0 ? <Button variant="primary" icon={WalletIcon} onClick={() => { setAmount(String(purchase.remaining)); setOpen(true); }}>{t("button")}</Button> : null}
    >
      <dl className="m-0 grid grid-cols-2 gap-3 text-sm">
        <div className="flex flex-col gap-0.5"><dt className="text-xs text-ink-muted">{t("paid")}</dt><dd className="m-0 font-bold">{formatEgp(purchase.paidAmount, locale)}</dd></div>
        <div className="flex flex-col gap-0.5">
          <dt className="text-xs text-ink-muted">{t("remaining")}</dt>
          <dd className={`m-0 font-bold ${purchase.remaining > 0 ? "text-orange-text" : ""}`}>{purchase.remaining > 0 ? formatEgp(purchase.remaining, locale) : t("settled")}</dd>
        </div>
      </dl>
      {open ? (
        <Dialog open onClose={() => setOpen(false)} title={t("dialogTitle")}>
          <form className="flex flex-col gap-3" onSubmit={(event) => { event.preventDefault(); if (valid) void confirm(); }}>
            <SelectField
              label={t("method")} name="method" value={method} onChange={(value) => setMethod(value as SellerPaymentMethod)}
              options={SELLER_PAYMENT_METHODS.map((value) => ({ value, label: t(`methods.${value}`) }))} error={submit.errors.fields.method}
            />
            <TextField label={t("amount")} name="amount" type="number" ltr value={amount} onChange={setAmount} error={submit.errors.fields.amount} />
            <p className="m-0 text-xs text-ink-muted">{t("hint", { amount: formatEgp(purchase.remaining, locale) })}</p>
            {valid ? <p className="m-0 text-sm">{t("confirmLine", { amount: formatEgp(Number(amount), locale), seller: purchase.seller.name, method: t(`methods.${method}`) })}</p> : null}
            <FormMessage message={submit.errors.general} />
            <div className="flex flex-wrap justify-end gap-2">
              <Button variant="ghost" onClick={() => setOpen(false)}>{t("cancel")}</Button>
              <Button type="submit" variant="primary" icon={WalletIcon} loading={submit.busy} disabled={!valid}>{t("confirm")}</Button>
            </div>
            {submit.dialog}
          </form>
        </Dialog>
      ) : null}
    </SectionCard>
  );
}
