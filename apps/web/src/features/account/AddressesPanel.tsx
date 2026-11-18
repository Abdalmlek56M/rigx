"use client";

import { MapPinIcon } from "@/components/ui/icons";
import { useState } from "react";
import { useTranslations } from "next-intl";
import { unwrap, type Schemas } from "@rigx/api-client";
import { useSession } from "@/features/auth/use-session";
import { browserApi } from "@/lib/api/browser";
import { fetchAddresses } from "./account-api";
import { AddressFormView } from "./AddressFormView";
import { addressLine, emptyAddressForm, formFromAddress, toAddressRequest, toUpdateRequest, type AddressForm } from "./address-form";
import { ConfirmButton } from "./ConfirmButton";
import { EmptyState, LoadBoundary } from "./LoadBoundary";
import { useLoad } from "@/lib/hooks/use-load";

type Address = Schemas["CustomerAddressView"];
type Editing = { kind: "new" } | { kind: "edit"; address: Address } | null;

const actionClass = "inline-flex min-h-11 items-center px-3 text-sm font-bold text-orange-text hover:underline";

function AddressCard({ address, onEdit, onDefault, onDelete }: { address: Address; onEdit: () => void; onDefault: () => void; onDelete: () => void }) {
  const t = useTranslations("account.addresses");
  return (
    <li className="flex flex-col gap-2 rounded-[10px] border border-line bg-surface p-5">
      <div className="flex flex-wrap items-center gap-2">
        <b dir="auto" className="[unicode-bidi:isolate]">{address.label}</b>
        {address.isDefault ? <span className="rounded-full bg-carbon px-3 py-1 text-xs font-bold text-on-carbon">{t("default")}</span> : null}
      </div>
      <p className="m-0 text-sm">
        <span dir="auto" className="[unicode-bidi:isolate]">{address.recipientName}</span> · <span dir="ltr" className="font-mono">{address.phoneNumber}</span>
      </p>
      <p dir="auto" className="m-0 text-sm text-ink-muted [unicode-bidi:isolate]">
        {address.governorate}، {addressLine(address, { floor: t("floorShort"), apartment: t("apartmentShort") })}
      </p>
      <div className="-mx-3 flex flex-wrap items-center">
        <button type="button" onClick={onEdit} className={actionClass}>{t("edit")}</button>
        {address.isDefault ? null : <button type="button" onClick={onDefault} className={actionClass}>{t("setDefault")}</button>}
        <ConfirmButton label={t("delete")} onConfirm={onDelete} />
      </div>
    </li>
  );
}

/** "العناوين": saved delivery addresses; add, edit, delete and pick the default one. */
export function AddressesPanel({ governorates }: { governorates: readonly string[] }) {
  const t = useTranslations("account.addresses");
  const session = useSession();
  const { state, reload } = useLoad(fetchAddresses);
  const [editing, setEditing] = useState<Editing>(null);
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);
  const user = session.status === "signedIn" ? session.user : null;

  async function run(action: () => Promise<unknown>): Promise<boolean> {
    setBusy(true);
    setFailed(false);
    try {
      await action();
      reload();
      return true;
    } catch {
      setFailed(true);
      return false;
    } finally {
      setBusy(false);
    }
  }

  async function save(form: AddressForm) {
    const api = browserApi();
    const current = editing;
    const done = await run(() =>
      current?.kind === "edit"
        ? unwrap(api.PUT("/api/v1/identity/addresses/{id}", { params: { path: { id: current.address.id } }, body: toUpdateRequest(form, current.address.rowVersion) }))
        : unwrap(api.POST("/api/v1/identity/addresses", { body: toAddressRequest(form) }))
    );
    if (done) setEditing(null);
  }

  const makeDefault = (address: Address) =>
    run(() => unwrap(browserApi().PUT("/api/v1/identity/addresses/{id}", {
      params: { path: { id: address.id } },
      body: toUpdateRequest({ ...formFromAddress(address), isDefault: true }, address.rowVersion)
    })));
  const remove = (address: Address) =>
    run(() => unwrap(browserApi().DELETE("/api/v1/identity/addresses/{id}", { params: { path: { id: address.id }, query: { rowVersion: address.rowVersion } } })));

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="m-0 text-xl font-bold">{t("title")}</h2>
        {editing ? null : <button type="button" onClick={() => setEditing({ kind: "new" })} className="h-11 rounded-control bg-orange px-5 font-bold text-on-orange">{t("add")}</button>}
      </div>
      {failed && !editing ? <p role="alert" className="m-0 text-sm text-error">{t("actionFailed")}</p> : null}
      {editing ? (
        <AddressFormView
          key={editing.kind === "edit" ? editing.address.id : "new"}
          initial={editing.kind === "edit" ? formFromAddress(editing.address) : emptyAddressForm({ name: user?.fullName ?? "", phone: user?.phoneNumber ?? "" }, false)}
          governorates={governorates} busy={busy} failed={failed}
          onSubmit={(form) => void save(form)} onCancel={() => { setEditing(null); setFailed(false); }}
        />
      ) : null}
      <LoadBoundary state={state} reload={reload}>
        {(addresses) =>
          addresses.length === 0 && !editing ? (
            <EmptyState icon={MapPinIcon} message={t("empty")} />
          ) : (
            <ul className="m-0 flex list-none flex-col gap-3 p-0">
              {addresses.map((address) => (
                <AddressCard
                  key={address.id} address={address} onEdit={() => setEditing({ kind: "edit", address })}
                  onDefault={() => void makeDefault(address)} onDelete={() => void remove(address)}
                />
              ))}
            </ul>
          )
        }
      </LoadBoundary>
    </div>
  );
}
