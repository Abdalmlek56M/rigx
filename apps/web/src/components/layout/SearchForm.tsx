import { useLocale, useTranslations } from "next-intl";
import { getPathname } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";

export function SearchForm() {
  const t = useTranslations("header");
  const locale = useLocale() as Locale;

  return (
    <form action={getPathname({ href: "/products", locale })} method="get" role="search" className="w-full md:w-[220px]">
      <label htmlFor="header-search" className="sr-only">
        {t("searchLabel")}
      </label>
      <input
        id="header-search"
        type="search"
        name="search"
        placeholder={t("searchPlaceholder")}
        className="h-[46px] w-full rounded-card border border-header-line bg-field px-3.5 text-[15px] text-on-carbon placeholder:text-muted md:h-11 md:rounded-control md:text-sm"
      />
    </form>
  );
}
