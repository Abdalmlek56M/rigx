import { useTranslations } from "next-intl";
import { Card } from "@/components/ui/Card";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { GearIcon, HelpIcon, InboxIcon, ShieldIcon, SwapIcon, WrenchIcon } from "@/components/ui/icons";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { textList, type ServiceDef, type ServiceId } from "./services-data";

const ICONS: Record<ServiceId, (p: { size?: number }) => React.JSX.Element> = {
  consult: HelpIcon,
  build: WrenchIcon,
  test: ShieldIcon,
  maintenance: GearIcon,
  sourcing: InboxIcon,
  upgrade: SwapIcon
};

interface ServiceCardProps {
  service: ServiceDef;
  /** Store WhatsApp number (digits) and the general template; without a number the card has no button. */
  whatsApp: { number: string | null; template: string };
}

/** One service: title, price, what it is, its steps, and a WhatsApp button with a prefilled line. */
export function ServiceCard({ service, whatsApp }: ServiceCardProps) {
  const t = useTranslations(`services.items.${service.id}`);
  const accent = service.tone === "accent";
  const ServiceIcon = ICONS[service.id];
  const text = [fillTemplate(whatsApp.template, {}), t("message")].filter(Boolean).join("\n");
  const steps = textList(t.raw("steps"));

  return (
    <Card
      as="article"
      id={`service-${service.id}`}
      title={t("title")}
      icon={ServiceIcon}
      padding="lg"
      className={`scroll-mt-28 gap-3.5 ${accent ? "accent-panel" : ""}`}
      actions={<StatusBadge tone="neutral">{t("price")}</StatusBadge>}
    >
      <p className="m-0 text-[15px] leading-[1.75] text-ink-muted">{t("body")}</p>
      <ol className="m-0 flex list-none flex-col gap-2 p-0 text-sm leading-[1.9] text-ink-muted">
        {steps.map((step, index) => (
          <li key={step} className="flex items-start gap-2.5">
            <span aria-hidden="true" className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-paper font-mono text-[11px] font-bold text-ink">{index + 1}</span>
            {step}
          </li>
        ))}
      </ol>
      {whatsApp.number ? (
        <WhatsAppButton href={whatsAppUrl(whatsApp.number, text)} className="mt-auto self-start">{t("cta")}</WhatsAppButton>
      ) : null}
    </Card>
  );
}
