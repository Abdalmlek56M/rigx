"use client";

import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import type { Brand } from "../brands/brands-api";

/** The brand dropdown of the stock forms (the registered brand list; none is allowed). */
export function BrandSelect({ brands, value, onChange, name = "brandId", error }: {
  brands: readonly Brand[];
  value: string;
  onChange: (id: string) => void;
  name?: string;
  error?: string | null;
}) {
  const t = useTranslations("admin.inventory.fields");
  return (
    <SelectField
      label={t("brand")} name={name} value={value} onChange={onChange} error={error}
      options={[{ value: "", label: t("noBrand") }, ...brands.map((brand) => ({ value: brand.id, label: brand.name }))]}
    />
  );
}
