import { getTranslations } from "next-intl/server";
import { fillTemplate, whatsAppUrl, NEW_TAB } from "@/lib/contact";

interface AskUsBannerProps {
  /** Store WhatsApp number (digits); without one the banner is not shown. */
  number: string | null;
  template: string;
}

/** "مش لاقي تجميعة مناسبة؟": the accent banner under the builds list, opening a WhatsApp chat with the store's message. */
export async function AskUsBanner({ number, template }: AskUsBannerProps) {
  const t = await getTranslations("builds.banner");
  if (!number) return null;
  const text = [fillTemplate(template, {}), t("message")].filter(Boolean).join("\n");

  return (
    <div className="flex flex-wrap items-center justify-between gap-[18px] accent-panel rounded-panel border px-6 py-7 md:px-8">
      <div className="flex flex-col gap-1.5">
        <span className="text-[22px] font-bold">{t("title")}</span>
        <span className="text-[15px] text-ink-muted">{t("body")}</span>
      </div>
      <a
        {...NEW_TAB} href={whatsAppUrl(number, text)}
        className="inline-flex h-[50px] items-center rounded-control bg-orange px-6 font-bold text-on-orange"
      >
        {t("cta")}
      </a>
    </div>
  );
}
