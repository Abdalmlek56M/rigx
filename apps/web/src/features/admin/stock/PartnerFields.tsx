"use client";

import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import type { Partner } from "../expenses/expenses-api";

/** Who financed a unit and with what share (empty percent = the partner's default). Hidden when the partners could not be loaded. */
export function PartnerFields({ partners, partnerId, percent, onChange, namePrefix = "partner", errors }: {
  partners: readonly Partner[];
  partnerId: string;
  percent: string;
  onChange: (patch: { partnerId?: string; percent?: string }) => void;
  namePrefix?: string;
  errors?: { partnerId?: string; percent?: string };
}) {
  const t = useTranslations("admin.inventory.fields");
  return (
    <>
      <SelectField
        label={t("partner")} name={`${namePrefix}Id`} value={partnerId} onChange={(id) => onChange({ partnerId: id, ...(id ? {} : { percent: "" }) })} error={errors?.partnerId}
        options={[{ value: "", label: t("noPartner") }, ...partners.map((partner) => ({ value: partner.id, label: partner.name }))]}
      />
      {partnerId ? (
        <TextField
          label={t("percent")} name={`${namePrefix}Percent`} type="number" ltr value={percent} onChange={(next) => onChange({ percent: next })}
          placeholder={t("percentDefault")} error={errors?.percent}
        />
      ) : null}
    </>
  );
}
