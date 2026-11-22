"use client";

import { useCallback, type ReactNode } from "react";
import { useLocale, useTranslations } from "next-intl";
import { orderStatusKey } from "@/features/tracking/tracking-steps";
import { formatDate, formatEgp } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { Dialog } from "../kit/Dialog";import { RequestTypeBadge } from "../requests/RequestBadges";
import { fetchCustomerViewAs } from "./customers-api";
import { CloseIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="flex flex-col gap-1.5">
      <h3 className="m-0 text-sm font-bold">{title}</h3>
      {children}
    </section>
  );
}

function Empty() {
  const t = useTranslations("admin.customers.viewAsSections");
  return <p className="m-0 text-xs text-ink-muted">{t("empty")}</p>;
}

/** Read-only "see it as the customer": profile, addresses, recent orders, warranties, invoices, requests and builds. */
export function ViewAsDialog({ id, onClose }: { id: string; onClose: () => void }) {
  const t = useTranslations("admin.customers");
  const sections = useTranslations("admin.customers.viewAsSections");
  const orderStatus = useTranslations("orderStatus");
  const common = useTranslations("common");
  const locale = useLocale();
  const load = useCallback(() => fetchCustomerViewAs(id), [id]);
  const { state } = useLoad(load);

  return (
    <Dialog open onClose={onClose} title={t("viewAsTitle")}>
      <p className="m-0 text-xs text-ink-muted">{t("viewAsNote")}</p>
      {state.status === "loading" ? <p className="m-0 text-sm">{t("loading")}</p> : null}
      {state.status === "error" ? <p role="alert" className="m-0 text-sm font-bold text-error-text">{t("failed")}</p> : null}
      {state.status === "ready" ? (
        <div className="flex max-h-[65vh] flex-col gap-4 overflow-y-auto pe-1">
          <Section title={sections("profile")}>
            <p className="m-0 text-sm"><b>{state.data.profile.fullName}</b></p>
            <p dir="ltr" className="m-0 font-mono text-xs text-ink-muted">{state.data.profile.email}</p>
            {state.data.profile.phoneNumber ? <p dir="ltr" className="m-0 font-mono text-xs text-ink-muted">{state.data.profile.phoneNumber}</p> : null}
          </Section>

          <Section title={sections("addresses")}>
            {state.data.addresses.length === 0 ? <Empty /> : (
              <ul className="m-0 flex list-none flex-col gap-1 p-0 text-sm">
                {state.data.addresses.map((address) => (
                  <li key={address.id}>
                    <b>{address.label}</b> · {address.governorate} · {address.city} · {address.street}
                  </li>
                ))}
              </ul>
            )}
          </Section>

          <Section title={sections("orders")}>
            {state.data.orders.length === 0 ? <Empty /> : (
              <ul className="m-0 flex list-none flex-col gap-1 p-0 text-sm">
                {state.data.orders.slice(0, 5).map((order) => (
                  <li key={order.id} className="flex flex-wrap items-center justify-between gap-2">
                    <span dir="ltr" className="font-mono text-xs text-orange-text">{order.reference}</span>
                    <span className="text-xs text-ink-muted">{orderStatus(orderStatusKey(order.status))}</span>
                    <b className="whitespace-nowrap">{formatEgp(order.total, locale)}</b>
                  </li>
                ))}
              </ul>
            )}
          </Section>

          <Section title={sections("warranties")}>
            {state.data.warranties.length === 0 ? <Empty /> : (
              <ul className="m-0 flex list-none flex-col gap-1 p-0 text-sm">
                {state.data.warranties.slice(0, 5).map((warranty) => (
                  <li key={`${warranty.orderId}-${warranty.code}`} className="flex flex-wrap items-center justify-between gap-2">
                    <span dir="auto" className="[unicode-bidi:isolate]">{warranty.name}</span>
                    <span dir="ltr" className="font-mono text-xs text-ink-muted">{warranty.code}</span>
                    {warranty.endsAtUtc ? <span className="text-xs text-ink-muted">{formatDate(warranty.endsAtUtc, locale)}</span> : null}
                  </li>
                ))}
              </ul>
            )}
          </Section>

          <Section title={sections("invoices")}>
            {state.data.invoices.length === 0 ? <Empty /> : (
              <ul className="m-0 flex list-none flex-col gap-1 p-0 text-sm">
                {state.data.invoices.slice(0, 5).map((invoice) => (
                  <li key={invoice.number} className="flex flex-wrap items-center justify-between gap-2">
                    <span dir="ltr" className="font-mono text-xs text-orange-text">{invoice.number}</span>
                    <b className="whitespace-nowrap">{formatEgp(invoice.total, locale)}</b>
                  </li>
                ))}
              </ul>
            )}
          </Section>

          <Section title={sections("coupons")}>
            {state.data.couponUses.length === 0 ? <Empty /> : (
              <ul className="m-0 flex list-none flex-col gap-1 p-0 text-sm">
                {state.data.couponUses.map((use) => (
                  <li key={use.orderId} className="flex flex-wrap items-center justify-between gap-2">
                    <span dir="ltr" className="font-mono text-xs font-bold">{use.code}</span>
                    <span dir="ltr" className="font-mono text-xs text-orange-text">{use.reference}</span>
                    <b className="whitespace-nowrap">{formatEgp(use.discount, locale)}</b>
                  </li>
                ))}
              </ul>
            )}
          </Section>

          <Section title={sections("requests")}>
            {state.data.requests.length === 0 ? <Empty /> : (
              <ul className="m-0 flex list-none flex-col gap-1 p-0 text-sm">
                {state.data.requests.map((request) => (
                  <li key={request.code} className="flex flex-wrap items-center justify-between gap-2">
                    <span dir="ltr" className="font-mono text-xs text-orange-text">{request.code}</span>
                    <RequestTypeBadge type={request.type} />
                    {request.offerAmount !== null ? <b className="whitespace-nowrap">{formatEgp(request.offerAmount, locale)}</b> : null}
                  </li>
                ))}
              </ul>
            )}
          </Section>

          <Section title={sections("builds")}>
            {state.data.builds.length === 0 ? <Empty /> : (
              <ul className="m-0 flex list-none flex-col gap-1 p-0 text-sm">
                {state.data.builds.map((build) => (
                  <li key={build.token} className="flex flex-wrap items-center justify-between gap-2">
                    <span>{build.name ?? t("none")}</span>
                    <span className="text-xs text-ink-muted">{t("partsCount", { count: build.partCount })}</span>
                  </li>
                ))}
              </ul>
            )}
          </Section>
        </div>
      ) : null}
      <div className="flex justify-end">
        <Button icon={CloseIcon} onClick={onClose}>{common("close")}</Button>
      </div>
    </Dialog>
  );
}
