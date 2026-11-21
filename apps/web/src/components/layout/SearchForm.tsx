import { useLocale, useTranslations } from "next-intl";
import { SearchIcon } from "@/components/ui/icons";
import { getPathname } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";

/** Header search: a rounded field with the search icon at its start; submits to the catalog. */
export function SearchForm() {
  const t = useTranslations("header");
  const locale = useLocale() as Locale;

  return (
    <form action={getPathname({ href: "/products", locale })} method="get" role="search" className="relative w-full min-w-0">
      <label htmlFor="header-search" className="sr-only">
        {t("searchLabel")}
      </label>
      <span aria-hidden="true" className="pointer-events-none absolute inset-y-0 start-4 flex items-center text-muted">
        <SearchIcon size={18} />
      </span>
      <input
        id="header-search"
        type="search"
        name="search"
        enterKeyHint="search"
        placeholder={t("searchPlaceholder")}
        className="h-11 w-full rounded-full border border-transparent bg-paper pe-4 ps-11 text-[15px] text-ink transition-[border-color,background-color,box-shadow] duration-200 placeholder:text-muted hover:border-line focus:border-orange focus:bg-surface focus:shadow-[0_0_0_4px_var(--orange-soft)] focus:outline-none"
      />
    </form>
  );
}
