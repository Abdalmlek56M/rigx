import { getTranslations } from "next-intl/server";
import { FaqList, type FaqEntry } from "@/components/ui/FaqList";

/** "قبل ما تشتري، اسأل براحتك": the general FAQs; the WhatsApp link only when the store has a number. */
export async function FaqSection({ entries, whatsAppHref }: { entries: FaqEntry[]; whatsAppHref: string | null }) {
  const t = await getTranslations("home.faq");
  if (entries.length === 0) return null;

  return (
    <section className="flex shell flex-wrap gap-10 pb-16">
      <div className="flex flex-1 basis-[320px] flex-col gap-3.5">
        <h2 className="m-0 text-[26px] leading-[1.35] font-bold md:text-[30px]">{t("title")}</h2>
        <p className="m-0 leading-[1.75] text-ink-muted">
          {whatsAppHref
            ? t.rich("body", {
                link: (chunks) => (
                  <a href={whatsAppHref} className="text-orange-text underline hover:text-orange-text-hover">
                    {chunks}
                  </a>
                )
              })
            : t("bodyNoLink")}
        </p>
      </div>
      <div className="flex-[2_1_560px]">
        <FaqList entries={entries} />
      </div>
    </section>
  );
}
