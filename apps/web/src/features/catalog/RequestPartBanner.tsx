import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";

/** The accent "can't find it?" banner under the catalog: sends the visitor to the services page to request a part. */
export function RequestPartBanner() {
  const t = useTranslations("catalog.banner");
  return (
    <div className="flex flex-wrap items-center justify-between gap-4 accent-panel rounded-card border px-7 py-6">
      <div className="flex flex-col gap-1">
        <span className="text-lg font-bold">{t("title")}</span>
        <span className="text-sm text-ink-muted">{t("body")}</span>
      </div>
      <Link href="/services" className="inline-flex h-12 items-center rounded-control bg-orange px-5 font-bold text-on-orange">
        {t("cta")}
      </Link>
    </div>
  );
}
