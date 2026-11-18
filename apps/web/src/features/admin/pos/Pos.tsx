"use client";

import { useEffect, useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { toFormErrors, type FormErrors } from "../kit/form-errors";
import { PageHeader } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import type { Order } from "../orders/orders-api";
import { usePermissions } from "../use-permissions";
import { createSale, previewSale, type PosPreview } from "./pos-api";
import { PosCart } from "./PosCart";
import { PosCustomer } from "./PosCustomer";
import { PosDelivery, type Governorate } from "./PosDelivery";
import { PosPayments } from "./PosPayments";
import { PosPicker } from "./PosPicker";
import { PosSold } from "./PosSold";
import { PosSummary } from "./PosSummary";
import type { SetForm } from "./pos-parts";
import { addToCart, EMPTY_POS_FORM, sendableLines, toSaleRequest, validateSale, type CartLine, type PosForm } from "./pos-rules";

const PREVIEW_DELAY_MS = 400;

/** One line for the whole form: the message of the failure, or its field messages together. */
const describe = (errors: FormErrors): string | null => errors.general ?? (Object.values(errors.fields).join(" · ") || null);

interface PosProps {
  leadSources: readonly string[];
  governorates: readonly Governorate[];
}

/** ADM-POS: sell a piece or a build over the counter: cart, customer, payment, the server's live pricing, then confirm or save pending. */
export function Pos({ leadSources, governorates }: PosProps) {
  const t = useTranslations("admin.pos");
  const kit = useTranslations("admin.kit");
  const { has } = usePermissions();
  const submit = useSubmit();
  const [cart, setCart] = useState<CartLine[]>([]);
  const [form, setForm] = useState<PosForm>(EMPTY_POS_FORM);
  const [preview, setPreview] = useState<PosPreview | null>(null);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [attempted, setAttempted] = useState(false);
  const [sold, setSold] = useState<Order | null>(null);

  const set: SetForm = (key, value) => setForm((current) => ({ ...current, [key]: value }));
  const priced = sendableLines(cart).length > 0;
  const total = priced && preview ? preview.total : null;
  const errors = attempted ? validateSale(cart, form, total) : [];
  const below = useMemo(
    () => new Set(priced && preview ? preview.lines.flatMap((line) => (line.belowCost && line.code ? [line.code] : [])) : []),
    [priced, preview]
  );

  // The server prices the cart as it changes; an answer for an older cart is dropped, a failure keeps the last numbers.
  const failed = kit("failed");
  const request = JSON.stringify(toSaleRequest(cart, form, true));
  useEffect(() => {
    if (!priced) return;
    let current = true;
    const timer = window.setTimeout(() => {
      previewSale(JSON.parse(request)).then(
        (answer) => {
          if (!current) return;
          setPreview(answer);
          setPreviewError(null);
        },
        (error) => current && setPreviewError(describe(toFormErrors(error, failed)))
      );
    }, PREVIEW_DELAY_MS);
    return () => {
      current = false;
      window.clearTimeout(timer);
    };
  }, [priced, request, failed]);

  function sell(confirm: boolean) {
    setAttempted(true);
    if (validateSale(cart, form, total).length > 0) return;
    void submit.run(async () => {
      setSold(await createSale(toSaleRequest(cart, form, confirm)));
    }, t("sold.savedToast"));
  }

  function reset() {
    setCart([]);
    setForm(EMPTY_POS_FORM);
    setPreview(null);
    setPreviewError(null);
    setAttempted(false);
    setSold(null);
  }

  return (
    <>
      <PageHeader title={t("title")} actions={<Link href="/admin/orders?channel=Store" className="inline-flex h-11 items-center rounded-control border border-input-line bg-surface px-4 text-sm font-bold">{t("recent")}</Link>} />
      {sold ? <PosSold order={sold} canDeliver={has("Sales.ApproveOrder")} onNewSale={reset} /> : (
        <div className="grid items-start gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
          <div className="flex min-w-0 flex-col gap-5">
            <PosPicker onAdd={(line) => setCart((current) => addToCart(current, line))} />
            <PosCart cart={cart} below={below} errors={errors} onChange={setCart} />
            <PosCustomer form={form} set={set} errors={errors} />
            <PosDelivery form={form} set={set} errors={errors} governorates={governorates} leadSources={leadSources} />
            <PosPayments form={form} set={set} errors={errors} total={total} />
          </div>
          <PosSummary
            lineCount={cart.length} preview={priced ? preview : null} previewError={priced ? previewError : null}
            busy={submit.busy} saleError={describe(submit.errors)} onSell={sell}
          />
        </div>
      )}
      {submit.dialog}
    </>
  );
}
