import { getTranslations } from "next-intl/server";
import { InterestButton } from "@/features/requests/InterestButton";
import { COMING_SOON } from "./services-data";

/** "لسه بنجهزها": services not open yet, each with an interest sign-up. */
export async function ComingSoon() {
  const t = await getTranslations("services.soon");

  return (
    <section className="flex shell flex-col gap-[18px] pb-[72px] pt-14">
      <div className="flex flex-col gap-1.5">
        <h2 className="m-0 text-[28px] font-bold">{t("title")}</h2>
        <span className="text-[15px] text-ink-muted">{t("subtitle")}</span>
      </div>
      <ul className="m-0 grid list-none gap-3.5 p-0 md:grid-cols-[repeat(auto-fit,minmax(260px,1fr))]">
        {COMING_SOON.map((id) => (
          <li key={id} className="flex flex-col gap-2 rounded-card border-2 border-dashed border-input-line p-[22px]">
            <span className="text-xs font-bold text-orange-text">{t("badge")}</span>
            <b className="text-lg">{t(`${id}.title`)}</b>
            <span className="text-sm leading-6 text-ink-muted">{t(`${id}.body`)}</span>
            <InterestButton
              subject={t(`${id}.title`)}
              label={t(`${id}.cta`)}
              className="min-h-11 self-start text-sm font-bold text-orange-text underline hover:text-orange-text-hover"
            />
          </li>
        ))}
      </ul>
    </section>
  );
}
