"use client";

import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { Link } from "@/i18n/navigation";
import { ExternalIcon, RefreshIcon } from "@/components/ui/icons";
import { Button, buttonClass, buttonIconSize } from "../kit/Button";
import { PartnersFailed } from "./PartnersFailed";
import { partnerChoices } from "./stock-fields";
import type { PartnerSource } from "./use-partners";

/** A whole row of the form grid it sits in (a dialog's column ignores it). */
const WIDE = "sm:col-span-full";

/**
 * Who financed a unit and with what share (empty percent = the partner's default). Left out only for staff who may not read
 * partners; a list that failed to load, or has nobody to pick, says why. `keepId` is the unit's saved partner.
 */
export function PartnerFields({ source, partnerId, percent, onChange, keepId = null, namePrefix = "partner", errors }: {
  source: PartnerSource | null;
  partnerId: string;
  percent: string;
  onChange: (patch: { partnerId?: string; percent?: string }) => void;
  keepId?: string | null;
  namePrefix?: string;
  errors?: { partnerId?: string; percent?: string };
}) {
  const t = useTranslations("admin.inventory.fields");
  const list = useTranslations("admin.inventory.partnerList");
  if (!source) return null;
  if (source.state.status === "error") return <PartnersFailed onRetry={source.reload} className={WIDE} />;
  const loading = source.state.status === "loading";
  const choices = source.state.status === "ready" ? partnerChoices(source.state.data, keepId) : [];
  const nobody = !loading && choices.every((choice) => choice.blocked !== null);
  return (
    <>
      <SelectField
        label={t("partner")} name={`${namePrefix}Id`} value={partnerId} disabled={loading} error={errors?.partnerId}
        onChange={(id) => onChange({ partnerId: id, ...(id ? {} : { percent: "" }) })}
        options={[
          { value: "", label: loading ? list("loading") : t("noPartner") },
          ...choices.map(({ partner, blocked }) => ({
            value: partner.id,
            label: blocked ? list(`blocked.${blocked}`, { name: partner.name }) : partner.name,
            disabled: blocked !== null
          }))
        ]}
      />
      {partnerId ? (
        <TextField
          label={t("percent")} name={`${namePrefix}Percent`} type="number" ltr value={percent} onChange={(next) => onChange({ percent: next })}
          placeholder={t("percentDefault")} error={errors?.percent}
        />
      ) : null}
      {nobody ? (
        <div role="status" className={`flex flex-wrap items-center justify-between gap-2 rounded-control bg-orange-soft px-3 py-2 ${WIDE}`}>
          <p className="m-0 min-w-0 flex-1 basis-60 text-sm">{list(choices.length === 0 ? "none" : "nobody")}</p>
          <span className="flex flex-wrap gap-2">
            {/* A new tab, so what is typed in this form stays. */}
            <Link href="/admin/partners" target="_blank" rel="noreferrer" className={buttonClass({ size: "sm" })}>
              <ExternalIcon size={buttonIconSize("sm")} />{list("open")}
            </Link>
            <Button size="sm" icon={RefreshIcon} onClick={source.reload}>{list("refresh")}</Button>
          </span>
        </div>
      ) : null}
    </>
  );
}
