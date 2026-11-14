import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";

/** The dark "can't find it?" banner under the catalog: sends the visitor to the services page to request a part. */
export function RequestPartBanner() {
  const t = useTranslations("catalog.banner");
  return (
    <div className="flex flex-wrap items-center justify-between gap-4 rounded-[10px] bg-carbon px-7 py-6 text-on-carbon">
      <div className="flex flex-col gap-1">
        <span className="text-lg font-bold">{t("title")}</span>
        <span className="text-sm text-footer-text">{t("body")}</span>
      </div>
      <Link href="/services" className="inline-flex h-12 items-center rounded-control bg-orange px-5 font-bold text-on-orange">
        {t("cta")}
      </Link>
    </div>
  );
}
