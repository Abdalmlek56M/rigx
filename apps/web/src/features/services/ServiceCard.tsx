import { useTranslations } from "next-intl";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { textList, type ServiceDef } from "./services-data";

interface ServiceCardProps {
  service: ServiceDef;
  /** Store WhatsApp number (digits) and the general template; without a number the card has no button. */
  whatsApp: { number: string | null; template: string };
}

/** One service: code and price, what it is, its steps, and a button that opens WhatsApp with a prefilled line. */
export function ServiceCard({ service, whatsApp }: ServiceCardProps) {
  const t = useTranslations(`services.items.${service.id}`);
  const dark = service.tone === "dark";
  const text = [fillTemplate(whatsApp.template, {}), t("message")].filter(Boolean).join("\n");

  return (
    <article className={`flex flex-col gap-3.5 rounded-panel border p-7 ${dark ? "border-carbon bg-carbon text-on-carbon" : "border-line bg-surface"}`}>
      <div className="flex items-center justify-between gap-3">
        <span dir="ltr" className={`font-mono text-[13px] font-semibold ${dark ? "text-orange" : "text-orange-text"}`}>{service.code}</span>
        <span className="text-end text-[13px] opacity-75">{t("price")}</span>
      </div>
      <h2 className="m-0 text-2xl font-bold">{t("title")}</h2>
      <p className="m-0 text-[15px] leading-[1.75] opacity-85">{t("body")}</p>
      <ol className="m-0 ps-5 text-sm leading-[1.9] opacity-85">
        {textList(t.raw("steps")).map((step) => (
          <li key={step}>{step}</li>
        ))}
      </ol>
      {whatsApp.number ? (
        <a
          href={whatsAppUrl(whatsApp.number, text)}
          target="_blank"
          rel="noopener noreferrer"
          className={`mt-auto inline-flex h-12 items-center self-start rounded-control px-[22px] font-bold ${dark ? "bg-orange text-on-orange" : "bg-carbon text-on-carbon"}`}
        >
          {t("cta")}
        </a>
      ) : null}
    </article>
  );
}
