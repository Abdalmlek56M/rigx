import { getLocale, getTranslations } from "next-intl/server";
import type { Schemas } from "@rigx/api-client";
import { Card } from "@/components/ui/Card";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { warrantyTone } from "@/components/ui/status-badge-rules";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { ShieldIcon } from "@/components/ui/icons";
import { warrantyStatusKey } from "@/features/account/account-labels";
import { parseCondition } from "@/lib/condition";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { formatDate } from "@/lib/format";
import { daysLeftToShow } from "./warranty-query";

export interface WarrantyWhatsApp {
  number: string | null;
  template: string;
}

async function WarrantyCard({ item, whatsApp }: { item: Schemas["WarrantyPublicView"]; whatsApp: WarrantyWhatsApp }) {
  const t = await getTranslations("account.warranties");
  const locale = await getLocale();
  const status = warrantyStatusKey(item.status);
  const condition = parseCondition(item.condition);
  const days = daysLeftToShow(item.status, item.daysLeft);
  const text = fillTemplate(whatsApp.template, { item: item.name, code: item.code })
    || t("whatsAppMessage", { name: item.name, code: item.code });

  return (
    <Card
      as="li"
      title={<span dir="auto" className="[unicode-bidi:isolate]">{item.name}</span>}
      icon={ShieldIcon}
      padding="md"
      className="gap-3"
      actions={status ? <StatusBadge tone={warrantyTone(status)}>{t(`status.${status}`)}</StatusBadge> : undefined}
    >
      <InfoGrid columns={2} className="gap-y-3">
        <InfoField label={t("code")} ltr value={item.code} />
        {condition ? <InfoField label={t("condition")} value={<ConditionBadge condition={condition} />} /> : null}
        {item.buildCode ? <InfoField label={t("build")} ltr value={item.buildCode} /> : null}
        <InfoField label={t("plan")} value={item.label} />
        <InfoField label={t("from")} value={formatDate(item.startsAtUtc, locale)} />
        <InfoField label={t("until")} value={item.endsAtUtc ? formatDate(item.endsAtUtc, locale) : undefined} />
        {days !== null ? <InfoField label={t("daysLeftLabel")} value={t("daysCount", { days })} className="text-success" /> : null}
      </InfoGrid>
      {whatsApp.number ? (
        <WhatsAppButton href={whatsAppUrl(whatsApp.number, text)} variant="outline" size="sm" className="self-start">{t("problem")}</WhatsAppButton>
      ) : null}
    </Card>
  );
}

/** The warranty of every item the search matched, one card each. */
export async function WarrantyResults({ items, whatsApp }: { items: Schemas["WarrantyPublicView"][]; whatsApp: WarrantyWhatsApp }) {
  return (
    <ul className={`m-0 grid list-none gap-3 p-0 ${items.length > 1 ? "md:grid-cols-2" : ""}`}>
      {items.map((item) => (
        <WarrantyCard key={item.code} item={item} whatsApp={whatsApp} />
      ))}
    </ul>
  );
}
