import { useTranslations } from "next-intl";
import { WhatsAppIcon } from "@/components/ui/icons";
import { fillTemplate, whatsAppUrl, NEW_TAB } from "@/lib/contact";
import { textList, type ServiceDef } from "./services-data";

interface ServiceCardProps {
  service: ServiceDef;
  /** Store WhatsApp number (digits) and the general template; without a number the card has no button. */
  whatsApp: { number: string | null; template: string };
}

/** One service: title and price, what it is, its steps, and a button that opens WhatsApp (new tab) with a prefilled line. */
export function ServiceCard({ service, whatsApp }: ServiceCardProps) {
  const t = useTranslations(`services.items.${service.id}`);
  const accent = service.tone === "accent";
  const text = [fillTemplate(whatsApp.template, {}), t("message")].filter(Boolean).join("\n");

  return (
    <article id={`service-${service.id}`} className={`flex scroll-mt-28 flex-col gap-3.5 rounded-panel border p-7 ${accent ? "accent-panel" : "border-line bg-surface"}`}>
      <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
        <h2 className="m-0 text-2xl font-bold">{t("title")}</h2>
        <span className={`rounded-full px-3 py-1 text-[13px] font-bold bg-orange-soft text-orange-text`}>{t("price")}</span>
      </div>
      <p className="m-0 text-[15px] leading-[1.75] opacity-85">{t("body")}</p>
      <ol className="m-0 ps-5 text-sm leading-[1.9] opacity-85">
        {textList(t.raw("steps")).map((step) => (
          <li key={step}>{step}</li>
        ))}
      </ol>
      {whatsApp.number ? (
        <a
          {...NEW_TAB} href={whatsAppUrl(whatsApp.number, text)}
          className={`press mt-auto inline-flex h-12 items-center gap-2 self-start rounded-full px-6 font-bold transition-[filter] hover:brightness-110 ${accent ? "bg-orange text-on-orange" : "bg-carbon text-on-carbon"}`}
        >
          <WhatsAppIcon size={18} />
          {t("cta")}
        </a>
      ) : null}
    </article>
  );
}
