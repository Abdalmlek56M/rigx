import { getTranslations } from "next-intl/server";
import { Card } from "@/components/ui/Card";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";

interface AskUsBannerProps {
  /** Store WhatsApp number (digits); without one the banner is not shown. */
  number: string | null;
  template: string;
}

/** "لم تجد تجميعة مناسبة؟": the accent banner under the builds list, opening a WhatsApp chat with the store's message. */
export async function AskUsBanner({ number, template }: AskUsBannerProps) {
  const t = await getTranslations("builds.banner");
  if (!number) return null;
  const text = [fillTemplate(template, {}), t("message")].filter(Boolean).join("\n");

  return (
    <Card padding="lg" className="accent-panel">
      <div className="flex flex-wrap items-center justify-between gap-[18px]">
        <div className="flex flex-col gap-1.5">
          <span className="text-[22px] font-bold">{t("title")}</span>
          <span className="text-[15px] text-ink-muted">{t("body")}</span>
        </div>
        <WhatsAppButton href={whatsAppUrl(number, text)}>{t("cta")}</WhatsAppButton>
      </div>
    </Card>
  );
}
