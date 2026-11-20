"use client";

import { useRef, useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { FormSection } from "@/features/checkout/form-parts";
import { SearchBox } from "../kit/FilterBar";
import { usePosError, type SetForm } from "./pos-parts";
import { searchCustomers, type PosCustomer as FoundCustomer } from "./pos-api";
import { customerSearchReady, posPhone, posPhoneValid, type PosField, type PosForm } from "./pos-rules";
import { CheckIcon, PlusIcon, UserIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

interface PosCustomerProps {
  form: PosForm;
  set: SetForm;
  errors: readonly PosField[];
  /** Takes the picked customer's phone and fills the fields the cashier left empty. */
  onPick: (customer: FoundCustomer) => void;
}

type Search = { status: "idle" | "busy" | "failed" } | { status: "done"; customers: FoundCustomer[] };
/** What the typed phone is: a past buyer's number, a new one, or not checked yet. */
type PhoneCheck = { phone: string; status: "busy" | "failed" } | { phone: string; status: "done"; customer: FoundCustomer | null };

function Notice({ tone, icon, children, action }: { tone: "known" | "new" | "offer"; icon: ReactNode; children: ReactNode; action?: ReactNode }) {
  const colors = tone === "known" ? "border-success/40 bg-success/10" : tone === "new" ? "border-line bg-paper" : "border-orange/40 bg-orange-soft";
  return (
    <div role="status" className={`flex flex-wrap items-center justify-between gap-3 rounded-control border px-3.5 py-2.5 text-sm ${colors}`}>
      <span className="flex min-w-0 items-center gap-2.5">
        <span aria-hidden className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-surface">{icon}</span>
        <span className="min-w-0">{children}</span>
      </span>
      {action}
    </div>
  );
}

/**
 * The customer step: search past buyers by phone or name and pick one (their details fill what is still empty), or type a new
 * customer, who is registered with the sale. A typed phone that belongs to a past buyer offers their details.
 */
export function PosCustomer({ form, set, errors, onPick }: PosCustomerProps) {
  const t = useTranslations("admin.pos.customer");
  const error = usePosError(errors);
  const [search, setSearch] = useState<Search>({ status: "idle" });
  const [check, setCheck] = useState<PhoneCheck | null>(null);
  const [used, setUsed] = useState<string | null>(null);
  /** Changing it empties the search box after a pick. */
  const [searchKey, setSearchKey] = useState(0);
  const latest = useRef("");
  const phone = posPhone(form.phone);
  const orders = (customer: FoundCustomer) => t("orders", { count: customer.orderCount });

  function find(text: string) {
    latest.current = text;
    if (!customerSearchReady(text)) return setSearch({ status: "idle" });
    setSearch({ status: "busy" });
    searchCustomers(text).then(
      (customers) => latest.current === text && setSearch({ status: "done", customers }),
      () => latest.current === text && setSearch({ status: "failed" })
    );
  }

  function checkPhone() {
    if (!posPhoneValid(form.phone) || (check?.phone === phone && check.status !== "failed")) return;
    setCheck({ phone, status: "busy" });
    const answer = (next: PhoneCheck) => setCheck((current) => (current?.phone === phone ? next : current));
    searchCustomers(phone).then(
      (customers) => answer({ phone, status: "done", customer: customers.find((customer) => customer.phone === phone) ?? null }),
      () => answer({ phone, status: "failed" })
    );
  }

  function pick(customer: FoundCustomer) {
    onPick(customer);
    setUsed(customer.phone);
    latest.current = "";
    setSearch({ status: "idle" });
    setSearchKey((key) => key + 1);
    setCheck({ phone: customer.phone, status: "done", customer });
  }

  const known = check?.phone === phone && check.status === "done" ? check.customer : undefined;

  return (
    <FormSection title={t("title")} hint={t("hint")}>
      <div className="flex flex-col gap-2">
        <SearchBox key={searchKey} label={t("search")} value="" onChange={find} />
        {search.status === "busy" ? <p role="status" className="m-0 text-sm text-ink-muted">{t("finding")}</p> : null}
        {search.status === "failed" ? <p role="alert" className="m-0 text-sm text-error">{t("failed")}</p> : null}
        {search.status === "done" && search.customers.length === 0 ? <p role="status" className="m-0 text-sm text-ink-muted">{t("noMatch")}</p> : null}
        {search.status === "done" && search.customers.length > 0 ? (
          <ul aria-label={t("results")} className="m-0 flex list-none flex-col gap-1.5 p-0">
            {search.customers.map((customer) => (
              <li key={customer.phone} className="flex flex-wrap items-center justify-between gap-x-4 gap-y-2 rounded-control border border-line px-3 py-2">
                <span className="flex min-w-0 items-center gap-2.5">
                  <span className="text-ink-muted"><UserIcon size={18} /></span>
                  <span className="flex min-w-0 flex-col">
                    <b className="truncate">{customer.name}</b>
                    <span className="text-xs text-ink-muted"><span dir="ltr">{customer.phone}</span> · {orders(customer)}</span>
                  </span>
                </span>
                <Button size="sm" icon={CheckIcon} onClick={() => pick(customer)}>{t("pick")}</Button>
              </li>
            ))}
          </ul>
        ) : null}
      </div>

      {known && used === phone ? (
        <Notice tone="known" icon={<span className="text-success"><CheckIcon size={18} /></span>}>
          <b>{t("registered")}</b> · {t("registeredHint", { orders: orders(known) })}
        </Notice>
      ) : known ? (
        <Notice tone="offer" icon={<span className="text-orange"><UserIcon size={18} /></span>} action={<Button size="sm" icon={CheckIcon} onClick={() => pick(known)}>{t("use")}</Button>}>
          {t("phoneKnown", { name: known.name, orders: orders(known) })}
        </Notice>
      ) : known === null ? (
        <Notice tone="new" icon={<PlusIcon size={18} />}><b>{t("new")}</b> · {t("newHint")}</Notice>
      ) : check?.phone === phone && check.status === "failed" ? <p role="alert" className="m-0 text-sm text-error">{t("failed")}</p> : null}

      <div className="grid gap-3.5 sm:grid-cols-2">
        <TextField label={t("name")} name="name" value={form.name} onChange={(value) => set("name", value)} autoComplete="off" required error={error("name")} />
        <div className="flex flex-col gap-2" onBlur={checkPhone}>
          <TextField label={t("phone")} name="phone" type="tel" ltr value={form.phone} autoComplete="off" required error={error("phone")} onChange={(value) => set("phone", value)} />
          <label className="flex min-h-11 items-center gap-2.5 text-sm">
            <input type="checkbox" checked={form.phoneHasWhatsApp} onChange={(event) => set("phoneHasWhatsApp", event.target.checked)} className="h-[18px] w-[18px] accent-carbon" />
            {t("whatsapp")}
          </label>
        </div>
        <TextField label={t("email")} name="email" type="email" ltr value={form.email} onChange={(value) => set("email", value)} autoComplete="off" error={error("email")} />
      </div>
    </FormSection>
  );
}
