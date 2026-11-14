import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";

const SERVICES = ["trade", "request", "assemble", "maintain"] as const;

/** Four service teasers, all leading to the services page. */
export function ServiceCards() {
  const t = useTranslations("home.services");
  return (
    <section className="mx-auto max-w-[1240px] px-4 py-12 md:px-6 md:py-16">
      <h2 className="sr-only">{t("title")}</h2>
      <ul className="m-0 grid list-none grid-cols-[repeat(auto-fit,minmax(240px,1fr))] gap-3.5 p-0">
        {SERVICES.map((service) => (
          <li key={service} className="flex">
            <Link href="/services" className="flex w-full flex-col gap-2 rounded-[10px] border border-line bg-surface p-[22px]">
              <b className="text-lg">{t(`${service}.title`)}</b>
              <span className="text-sm leading-[1.6] text-ink-muted">{t(`${service}.body`)}</span>
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
