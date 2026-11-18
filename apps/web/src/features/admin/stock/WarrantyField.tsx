"use client";

import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";

/**
 * The warranty label. The server accepts only the labels in the store settings, so when the staff member may read them
 * (`options` loaded) it is a list; otherwise the label is typed and the server checks it.
 */
export function WarrantyField({ value, onChange, options, name = "warrantyLabel", error, emptyLabel }: {
  value: string;
  onChange: (label: string) => void;
  options: readonly string[] | null;
  name?: string;
  error?: string | null;
  /** What the empty choice means where "no warranty" is not it (a build's empty warranty is "each part's own"). */
  emptyLabel?: string;
}) {
  const t = useTranslations("admin.inventory.fields");
  if (options === null) return <TextField label={t("warranty")} name={name} value={value} onChange={onChange} error={error} />;
  const labels = value && !options.includes(value) ? [value, ...options] : options;
  return (
    <SelectField
      label={t("warranty")} name={name} value={value} onChange={onChange} error={error}
      options={[{ value: "", label: emptyLabel ?? t("noWarranty") }, ...labels.map((label) => ({ value: label, label }))]}
    />
  );
}
