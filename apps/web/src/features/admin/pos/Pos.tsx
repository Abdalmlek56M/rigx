"use client";

import { useEffect, useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { listSeparator } from "@/lib/format";
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
import { PosShortcutsDialog } from "./PosShortcutsDialog";
import { PosSummary } from "./PosSummary";
import { isTypingTarget, posShortcut } from "./pos-shortcuts";
import type { SetForm } from "./pos-parts";
import { addToCart, EMPTY_POS_FORM, sendableLines, toSaleRequest, validateSale, withCustomer, type CartLine, type PosForm } from "./pos-rules";
import { ClockIcon, KeyboardIcon } from "@/components/ui/icons";
import { buttonClass, buttonIconSize, IconButton } from "../kit/Button";

const PREVIEW_DELAY_MS = 400;

/** One line for the whole form: the message of the failure, or its field messages together. */
const describe = (errors: FormErrors, locale: string): string | null => errors.general ?? (Object.values(errors.fields).join(listSeparator(locale)) || null);

interface PosProps {
  leadSources: readonly string[];
  governorates: readonly Governorate[];
}

/** ADM-POS: sell a piece or a build over the counter: cart, customer, payment, the server's live pricing, then confirm or save pending. */
export function Pos({ leadSources, governorates }: PosProps) {
  const t = useTranslations("admin.pos");
  const locale = useLocale();
  const kit = useTranslations("admin.kit");
  const { has } = usePermissions();
  const submit = useSubmit();
  const [cart, setCart] = useState<CartLine[]>([]);
  const [form, setForm] = useState<PosForm>(EMPTY_POS_FORM);
  const [preview, setPreview] = useState<PosPreview | null>(null);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [attempted, setAttempted] = useState(false);
  const [sold, setSold] = useState<Order | null>(null);
  const [helpOpen, setHelpOpen] = useState(false);

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
        (error) => current && setPreviewError(describe(toFormErrors(error, failed), locale))
      );
    }, PREVIEW_DELAY_MS);
    return () => {
      current = false;
      window.clearTimeout(timer);
    };
  }, [priced, request, failed, locale]);

  function sell(confirm: boolean) {
    setAttempted(true);
    if (validateSale(cart, form, total).length > 0) return;
    void submit.run(async () => {
      setSold(await createSale(toSaleRequest(cart, form, confirm)));
    }, t("sold.savedToast"));
  }

  // Keyboard shortcuts of the counter; the handler is replaced on every render, so it always sees the current cart and form.
  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      const shortcut = posShortcut(event, isTypingTarget(event.target));
      if (!shortcut || sold) return;
      event.preventDefault();
      if (shortcut === "help") setHelpOpen(true);
      else if (shortcut === "focusItems" || shortcut === "focusCustomer") {
        const field = document.querySelector<HTMLInputElement>(`[data-focus="${shortcut === "focusItems" ? "pos-items" : "pos-customer"}"]`);
        field?.focus();
        field?.select();
      } else if (cart.length > 0 && !submit.busy) sell(shortcut === "confirm");
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  });

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
      <PageHeader
        title={t("title")}
        actions={(
          <>
            <IconButton icon={KeyboardIcon} label={t("shortcuts.title")} variant="secondary" aria-haspopup="dialog" onClick={() => setHelpOpen(true)} />
            <Link href="/admin/orders?channel=Store" className={buttonClass()}><ClockIcon size={buttonIconSize()} />{t("recent")}</Link>
          </>
        )}
      />
      <PosShortcutsDialog open={helpOpen} onClose={() => setHelpOpen(false)} />
      {sold ? <PosSold order={sold} canDeliver={has("Sales.ApproveOrder")} onNewSale={reset} /> : (
        <div className="grid items-start gap-5 xl:grid-cols-[minmax(0,1fr)_360px]">
          <div className="flex min-w-0 flex-col gap-5">
            <PosPicker onAdd={(line) => setCart((current) => addToCart(current, line))} />
            <PosCart cart={cart} below={below} errors={errors} onChange={setCart} />
            <PosCustomer form={form} set={set} errors={errors} onPick={(customer) => setForm((current) => withCustomer(current, customer))} />
            <PosDelivery form={form} set={set} errors={errors} governorates={governorates} leadSources={leadSources} />
            <PosPayments form={form} set={set} errors={errors} total={total} />
          </div>
          <PosSummary
            lineCount={cart.length} preview={priced ? preview : null} previewError={priced ? previewError : null}
            busy={submit.busy} saleError={describe(submit.errors, locale)} onSell={sell}
            couponCode={form.couponCode} onCouponCode={(code) => set("couponCode", code)}
          />
        </div>
      )}
      {submit.dialog}
    </>
  );
}
