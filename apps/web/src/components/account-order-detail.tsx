"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { getMyOrder, type StoreOrder } from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { AccountShell } from "@/components/account-shell";
import { LoadingCard, Message, PageIntro } from "@/components/ui";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";

export function AccountOrderDetail({ id }: { id: string }) {
  const { messages, locale } = useLocale();
  const [order, setOrder] = useState<StoreOrder | null>(null);
  const [error, setError] = useState(false);
  useEffect(() => {
    void getMyOrder(id).then(setOrder).catch(() => setError(true));
  }, [id]);
  return (
    <AccountShell>
      <PageIntro eyebrow="RIGX / ORDER" title={messages.accountOrderDetail} lead={order?.reference ?? ""} />
      {error ? <Message tone="error">{messages.videoUnavailable}</Message> : null}
      {!order && !error ? <LoadingCard /> : null}
      {order ? (
        <section className="rigx-admin-card grid gap-4 p-4">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <strong dir="ltr">{order.reference}</strong>
            <span className="rounded-full border border-[var(--ds-border)] px-2.5 py-1 text-xs font-bold">{order.status} · {order.paymentStatus}</span>
          </div>
          <p className="text-sm text-[var(--ds-text-muted)]">
            {order.fulfillmentMethod} · {order.paymentMethod} · <span dir="ltr">{new Date(order.createdAtUtc).toLocaleString("en-GB")}</span>
          </p>
          {order.addressText ? <p className="text-sm">{order.addressText}</p> : null}
          <ul className="grid gap-2">
            {order.items.map((item) => (
              <li key={item.id} className="grid gap-1 border-b border-[var(--ds-border)] pb-2 text-sm">
                <div className="flex flex-wrap justify-between gap-2">
                  <span>{item.name}{item.variantName ? ` · ${item.variantName}` : ""} × {item.quantity}</span>
                  <span dir="ltr">{formatCatalogPrice(item.lineTotal)} {currencyLabel(locale)}</span>
                </div>
                <span className="text-xs text-[var(--ds-text-muted)]" dir="ltr">{item.sku}</span>
              </li>
            ))}
          </ul>
          <div className="flex justify-between font-bold"><span>{messages.pcBuildTotal}</span><span dir="ltr">{formatCatalogPrice(order.total)} {currencyLabel(locale)}</span></div>
          <div className="grid gap-1 text-xs text-[var(--ds-text-muted)]">
            {order.events.map((event, index) => <span key={`${event.createdAtUtc}-${index}`}>{event.eventType} · {new Date(event.createdAtUtc).toLocaleString("en-GB")}</span>)}
          </div>
          <Link href="/account/orders" className="rigx-cart-text-link">{messages.accountOrdersSection}</Link>
        </section>
      ) : null}
    </AccountShell>
  );
}

