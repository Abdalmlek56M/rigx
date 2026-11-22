"use client";

import { useId, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ChevronDownIcon } from "@/components/ui/icons";
import { formatEgp } from "@/lib/format";
import type { Order } from "./account-api";

/**
 * The order's lines opened in place in the orders list (name, code, quantity and price), so a customer can check what they bought without
 * leaving the list; the full order page stays one click away.
 */
export function OrderItemsAccordion({ order }: { order: Order }) {
  const t = useTranslations("account.orders");
  const locale = useLocale();
  const [open, setOpen] = useState(false);
  const panelId = useId();

  return (
    <div className="flex flex-col gap-2">
      <button
        type="button" aria-expanded={open} aria-controls={panelId} onClick={() => setOpen(!open)}
        className="press flex min-h-11 w-full items-center justify-between gap-2 rounded-control border border-line bg-paper px-3 text-start text-sm font-bold"
      >
        {open ? t("hideItems") : t("showItems", { count: order.items.length })}
        <span aria-hidden="true" className={`flex shrink-0 transition-transform duration-200 motion-reduce:transition-none ${open ? "rotate-180" : ""}`}>
          <ChevronDownIcon size={16} />
        </span>
      </button>
      <ul id={panelId} hidden={!open} className="m-0 list-none flex-col gap-2 p-0 text-sm [&:not([hidden])]:flex">
        {order.items.map((item, index) => (
          <li key={`${item.code ?? item.name}-${index}`} className="flex items-baseline justify-between gap-3 border-b border-dashed border-line pb-2 last:border-b-0 last:pb-0">
            <span dir="auto" className="min-w-0 [unicode-bidi:isolate]">
              {item.name}
              {item.code ? <span dir="ltr" className="ms-1.5 font-mono text-xs text-ink-muted">{item.code}</span> : null}
              {item.quantity > 1 ? <span dir="ltr" className="ms-1.5 font-mono text-xs text-ink-muted">× {item.quantity}</span> : null}
            </span>
            <span className="shrink-0 tabular-nums">{formatEgp(item.lineTotal, locale)}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
