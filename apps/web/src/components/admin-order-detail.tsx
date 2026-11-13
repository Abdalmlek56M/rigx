"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { getAdminOrder, type AdminOrderUnit, type StoreOrder } from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro } from "@/components/ui";

export function AdminOrderDetail({ id }: { id: string }) {
  const { messages } = useLocale();
  const [order, setOrder] = useState<StoreOrder | null>(null);
  const [units, setUnits] = useState<AdminOrderUnit[]>([]);
  const [error, setError] = useState(false);
  useEffect(() => {
    void getAdminOrder(id).then((data) => { setOrder(data.order); setUnits(data.units); }).catch(() => setError(true));
  }, [id]);
  return (
    <main className="p-4 sm:p-6 lg:p-8"><div className="mx-auto grid max-w-5xl gap-5">
      <PageIntro eyebrow="RIGX / ORDER" title={messages.accountOrderDetail} lead={order?.reference ?? ""} />
      {error ? <Message tone="error">{messages.videoUnavailable}</Message> : null}
      {!order && !error ? <LoadingCard /> : null}
      {order ? (
        <section className="rigx-admin-card grid gap-3 p-4">
          <div className="flex flex-wrap justify-between gap-2"><strong dir="ltr">{order.reference}</strong><span className="text-xs font-bold">{order.status} · {order.paymentStatus}</span></div>
          <p className="text-sm">{order.customerName} · <span dir="ltr">{order.phoneNumber}</span></p>
          <p className="text-sm text-[var(--ds-text-muted)]">{order.fulfillmentMethod} · {order.paymentMethod}</p>
          {order.addressText ? <p className="text-sm">{order.addressText}</p> : null}
          <ul className="grid gap-2">
            {order.items.map((item) => (
              <li key={item.id} className="grid gap-1 border-b border-[var(--ds-border)] pb-2 text-sm">
                <div className="flex flex-wrap justify-between gap-2"><span>{item.name}{item.variantName ? ` · ${item.variantName}` : ""} × {item.quantity}</span><span dir="ltr">{item.lineTotal.toLocaleString()}</span></div>
                <span className="text-xs text-[var(--ds-text-muted)]" dir="ltr">{item.sku}</span>
              </li>
            ))}
          </ul>
          <h2 className="font-bold">{messages.adminInventoryNav ?? "Inventory units"}</h2>
          <ul className="grid gap-1 text-sm">
            {units.map((unit) => <li key={unit.unitId} className="flex flex-wrap justify-between gap-2"><span>{unit.name} · {unit.condition}</span><span dir="ltr">{unit.serialNumber ?? ""} · {unit.status}</span></li>)}
          </ul>
          <Link href="/admin/orders" className="rigx-cart-text-link">{messages.retailOrdersTitle}</Link>
        </section>
      ) : null}
    </div></main>
  );
}
