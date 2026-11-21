"use client";

import { CheckIcon, MapPinIcon, PencilIcon, PhoneIcon, PlusIcon, TrashIcon, UserIcon } from "@/components/ui/icons";
import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { unwrap, type Schemas } from "@rigx/api-client";
import { useSession } from "@/features/auth/use-session";
import { browserApi } from "@/lib/api/browser";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { localizedName } from "@/lib/localized-name";
import { fetchAddresses } from "./account-api";
import { AddressFormView } from "./AddressFormView";
import { addressLine, emptyAddressForm, formFromAddress, toAddressRequest, toUpdateRequest, type AddressForm, type GovernorateOption } from "./address-form";
import { ConfirmButton } from "./ConfirmButton";
import { EmptyState, LoadBoundary } from "./LoadBoundary";
import { RowButton } from "./RowAction";
import { useLoad } from "@/lib/hooks/use-load";

type Address = Schemas["CustomerAddressView"];
type Editing = { kind: "new" } | { kind: "edit"; address: Address } | null;

function AddressCard({ address, governorates, onEdit, onDefault, onDelete }: { address: Address; governorates: readonly GovernorateOption[]; onEdit: () => void; onDefault: () => void; onDelete: () => void }) {
  const t = useTranslations("account.addresses");
  const locale = useLocale();
  const line = addressLine(address, { floor: t("floorShort"), apartment: t("apartmentShort") });
  const governorate = governorates.find((entry) => entry.name === address.governorate) ?? { name: address.governorate, nameEn: null };
  return (
    <Card
      as="li"
      title={<span dir="auto" className="[unicode-bidi:isolate]">{address.label}</span>}
      icon={MapPinIcon}
      padding="md"
      className="gap-3"
      actions={
        <div className="flex items-center gap-1.5">
          {address.isDefault ? <StatusBadge tone="success" icon={CheckIcon}>{t("default")}</StatusBadge> : <RowButton label={t("setDefault")} icon={CheckIcon} onClick={onDefault} />}
          <RowButton label={t("edit")} icon={PencilIcon} onClick={onEdit} />
          <ConfirmButton iconOnly label={t("delete")} icon={TrashIcon} onConfirm={onDelete} />
        </div>
      }
    >
      <InfoGrid columns={2} className="gap-y-3">
        <InfoField label={t("recipientName")} icon={UserIcon} value={address.recipientName} />
        <InfoField label={t("phone")} icon={PhoneIcon} ltr value={address.phoneNumber} />
        <InfoField label={t("address")} icon={MapPinIcon} value={[localizedName(governorate, locale), line].filter(Boolean).join("، ")} wide />
      </InfoGrid>
    </Card>
  );
}

/** "العناوين": saved delivery addresses; add, edit, delete and pick the default one. */
export function AddressesPanel({ governorates }: { governorates: readonly GovernorateOption[] }) {
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
        <h2 className="sr-only">{t("title")}</h2>
        {editing ? null : <Button type="button" variant="accent" icon={PlusIcon} onClick={() => setEditing({ kind: "new" })}>{t("add")}</Button>}
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
                  key={address.id} address={address} governorates={governorates} onEdit={() => setEditing({ kind: "edit", address })}
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
