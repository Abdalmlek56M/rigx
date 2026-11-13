"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { getMyOrders, type StoreOrder } from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { AccountShell } from "@/components/account-shell";
import { LoadingCard, Message, PageIntro } from "@/components/ui";
import { currencyLabel, formatCatalogPrice } from "@/lib/storefront";

export function AccountOrders() {
  const { messages, locale } = useLocale();
  const [orders, setOrders] = useState<StoreOrder[]>([]);
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    void getMyOrders().then(setOrders).catch(() => setOrders([])).finally(() => setLoading(false));
  }, []);
  return (
    <AccountShell>
      <PageIntro eyebrow="RIGX / ORDERS" title={messages.accountOrdersSection} lead={messages.accountOrdersLead} />
      {loading ? <LoadingCard /> : orders.length === 0 ? <Message tone="info">{messages.accountOrdersEmpty}</Message> : (
        <section className="grid gap-4">
          {orders.map((order) => (
            <article key={order.id} className="rigx-admin-card grid gap-2 p-4">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <Link href={`/account/orders/${order.id}`} className="font-bold" dir="ltr">{order.reference}</Link>
                <span className="rounded-full border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] px-2.5 py-1 text-xs font-bold">{order.status} · {order.paymentStatus}</span>
              </div>
              <p className="text-sm text-[var(--ds-text-muted)]" dir="ltr">{new Date(order.createdAtUtc).toLocaleString("en-GB")}</p>
              <ul className="grid gap-1 text-sm">
                {order.items.map((item) => <li key={item.id} className="flex justify-between gap-2"><span>{item.name} × {item.quantity}</span><span dir="ltr">{formatCatalogPrice(item.lineTotal)} {currencyLabel(locale)}</span></li>)}
              </ul>
              <p className="font-bold">{messages.pcBuildTotal}: {formatCatalogPrice(order.total)} {currencyLabel(locale)}</p>
            </article>
          ))}
        </section>
      )}
    </AccountShell>
  );
}

