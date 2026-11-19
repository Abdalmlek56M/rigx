"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { orderStatusKey } from "@/features/tracking/tracking-steps";
import { Link } from "@/i18n/navigation";
import { formatDate, formatDateTime, formatEgp } from "@/lib/format";
import type { Load } from "@/lib/hooks/use-load";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { DataTable, type Column } from "../kit/DataTable";import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { RequestStatusBadge, RequestTypeBadge } from "../requests/RequestBadges";
import { saveCustomerNote, type CustomerProfile } from "./customers-api";
import { customerTab, orderChannelKey, warrantyStateKey } from "./customer-rules";

type Order = CustomerProfile["sales"]["orders"][number];
type Warranty = CustomerProfile["sales"]["warranties"][number];
type Request = CustomerProfile["requests"][number];

const tabButton = "h-10 whitespace-nowrap rounded-control px-3.5 text-[13px]";

function NoteEditor({ id, note, canManage, onSaved }: { id: string; note: string | null; canManage: boolean; onSaved: () => void }) {
  const t = useTranslations("admin.customers.notes");
  const submit = useSubmit();
  const [value, setValue] = useState(note ?? "");
  if (!canManage) return <p className="m-0 whitespace-pre-wrap text-sm text-ink-muted">{note || t("empty")}</p>;
  return (
    <form
      className="flex flex-col gap-3"
      onSubmit={(event) => {
        event.preventDefault();
        void submit.run(async () => {
          await saveCustomerNote(id, value);
          onSaved();
        }, t("saved"));
      }}
    >
      <TextAreaField label={t("title")} name="note" value={value} onChange={setValue} maxLength={2000} hint={t("hint")} error={submit.errors.fields.note} />
      <FormMessage message={submit.errors.general} />
      <button type="submit" disabled={submit.busy} className="h-11 self-start rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{t("save")}</button>
      {submit.dialog}
    </form>
  );
}

/** The customer's orders, warranties, requests and the internal note. */
export function CustomerActivityTabs({ profile, canManage, onRetry, onChanged }: {
  profile: CustomerProfile;
  canManage: boolean;
  onRetry: () => void;
  onChanged: () => void;
}) {
  const t = useTranslations("admin.customers");
  const orderStatus = useTranslations("orderStatus");
  const warrantyT = useTranslations("admin.customers.warranties");
  const locale = useLocale();
  const [rawTab, setRawTab] = useState("orders");
  const tab = customerTab(rawTab);
  const { orders, warranties } = profile.sales;
  const requests = profile.requests;
  const channelLabels = { Online: t("orders.online"), Store: t("orders.store"), Other: t("none") } as const;

  const orderColumns: Column<Order>[] = [
    { key: "reference", header: t("orders.reference"), cell: (row) => <span dir="ltr" className="font-mono text-xs text-orange-text">{row.reference}</span> },
    { key: "date", header: t("orders.date"), cell: (row) => <span dir="ltr" className="whitespace-nowrap font-mono text-xs">{formatDateTime(row.createdAtUtc, locale)}</span> },
    { key: "channel", header: t("orders.channel"), cell: (row) => channelLabels[orderChannelKey(row.channel)] },
    { key: "total", header: t("orders.total"), align: "end", cell: (row) => <span className="whitespace-nowrap font-bold">{formatEgp(row.total, locale)}</span> },
    { key: "paid", header: t("orders.paid"), align: "end", cell: (row) => <span className="whitespace-nowrap">{formatEgp(row.paidAmount, locale)}</span> },
    { key: "status", header: t("orders.status"), cell: (row) => <span className="inline-block whitespace-nowrap rounded-[4px] bg-paper px-2 py-[3px] text-xs font-bold">{orderStatus(orderStatusKey(row.status))}</span> }
  ];
  const warrantyColumns: Column<Warranty>[] = [
    { key: "code", header: warrantyT("code"), cell: (row) => <span dir="ltr" className="font-mono text-xs text-orange-text">{row.code}</span> },
    { key: "name", header: warrantyT("name"), cell: (row) => <span dir="auto" className="font-bold [unicode-bidi:isolate]">{row.name}</span> },
    { key: "ends", header: warrantyT("ends"), cell: (row) => <span className="whitespace-nowrap">{row.endsAtUtc ? warrantyT("until", { date: formatDate(row.endsAtUtc, locale) }) : t("none")}</span> },
    { key: "status", header: warrantyT("status"), cell: (row) => <WarrantyBadge status={row.status} /> }
  ];
  const requestColumns: Column<Request>[] = [
    { key: "code", header: t("requests.code"), cell: (row) => <Link href={`/admin/requests?id=${row.id}`} dir="ltr" className="font-mono text-xs text-orange-text hover:underline">{row.code}</Link> },
    { key: "type", header: t("requests.type"), cell: (row) => <RequestTypeBadge type={row.type} /> },
    { key: "status", header: t("requests.status"), cell: (row) => <RequestStatusBadge status={row.status} /> },
    { key: "date", header: t("requests.date"), cell: (row) => <span dir="ltr" className="whitespace-nowrap font-mono text-xs">{formatDateTime(row.createdAtUtc, locale)}</span> }
  ];
  const ready = <T,>(rows: readonly T[]): Load<readonly T[]> => ({ status: "ready", data: rows });

  return (
    <section className="flex min-w-0 flex-col gap-3.5 rounded-card border border-line bg-surface p-5">
      <div role="group" aria-label={t("detailTitle")} className="flex flex-wrap gap-1 rounded-card border border-line bg-surface p-1">
        {(["orders", "warranties", "requests", "notes"] as const).map((key) => {
          const count = key === "orders" ? orders.length : key === "warranties" ? warranties.length : key === "requests" ? requests.length : null;
          return (
            <button
              key={key} type="button" aria-pressed={tab === key} onClick={() => setRawTab(key)}
              className={`${tabButton} ${tab === key ? "bg-carbon font-bold text-on-carbon" : "font-medium hover:bg-line"}`}
            >
              {count === null ? t("tabs.notes") : t(`tabs.${key}`, { count })}
            </button>
          );
        })}
      </div>

      {tab === "orders" ? <DataTable label={t("tabs.orders", { count: orders.length })} columns={orderColumns} state={ready(orders)} onRetry={onRetry} rowKey={(row) => row.id} empty={t("orders.empty")} /> : null}
      {tab === "warranties" ? <DataTable label={t("tabs.warranties", { count: warranties.length })} columns={warrantyColumns} state={ready(warranties)} onRetry={onRetry} rowKey={(row) => row.id} empty={t("warranties.empty")} /> : null}
      {tab === "requests" ? <DataTable label={t("tabs.requests", { count: requests.length })} columns={requestColumns} state={ready(requests)} onRetry={onRetry} rowKey={(row) => row.id} empty={t("requests.empty")} /> : null}
      {tab === "notes" ? <NoteEditor key={`${profile.account.id}-${profile.account.note ?? ""}`} id={profile.account.id} note={profile.account.note} canManage={canManage} onSaved={onChanged} /> : null}
    </section>
  );
}

function WarrantyBadge({ status }: { status: string }) {
  const t = useTranslations("admin.customers.warranties");
  const key = warrantyStateKey(status);
  const tone = key === "Active" ? "bg-success text-surface" : key === "Void" ? "bg-error text-surface" : "bg-line text-ink-muted";
  const label = key === "Active" ? t("active") : key === "Expired" ? t("expired") : key === "Void" ? t("void") : t("unknown");
  return <span className={`inline-block whitespace-nowrap rounded-[4px] px-2 py-[3px] text-xs font-bold ${tone}`}>{label}</span>;
}
