import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { UserIcon } from "@/components/ui/icons";
import { Logo } from "@/components/ui/Logo";
import { CartButton } from "@/features/cart/CartButton";
import type { StorefrontData } from "@/lib/storefront";
import { CategoriesMenu } from "./CategoriesMenu";
import { LocaleSwitch } from "./LocaleSwitch";
import { MobileMenu } from "./MobileMenu";
import { NAV_LINKS } from "./nav-links";
import { SearchForm } from "./SearchForm";
import { ThemeToggle } from "./ThemeToggle";

export function Header({ storefront }: { storefront: StorefrontData }) {
  const t = useTranslations("header");

  return (
    <header className="bg-carbon text-on-carbon">
      <div className="mx-auto flex max-w-[1240px] flex-wrap items-center justify-between gap-x-7 gap-y-3 px-4 py-3 md:justify-start md:px-6 md:py-[14px]">
        <MobileMenu categories={storefront.categories} englishEnabled={storefront.englishEnabled} />
        <Link href="/" aria-label={t("home")} className="flex">
          <Logo />
        </Link>
        <nav aria-label={t("navLabel")} className="hidden flex-1 items-center gap-x-5 gap-y-1 md:flex">
          <CategoriesMenu categories={storefront.categories} />
          {NAV_LINKS.map((link) => (
            <Link
              key={link.key}
              href={link.href}
              className={`flex min-h-11 items-center whitespace-nowrap text-[15px] ${link.emphasis ? "font-bold text-orange" : ""}`}
            >
              {t(link.key)}
            </Link>
          ))}
        </nav>
        <div className="order-last w-full md:order-none md:w-auto">
          <SearchForm />
        </div>
        <div className="flex items-center gap-2">
          <Link
            href="/account"
            aria-label={t("account")}
            className="hidden h-11 w-11 items-center justify-center rounded-control border border-header-line md:flex"
          >
            <UserIcon />
          </Link>
          <ThemeToggle className="max-md:hidden" />
          {storefront.englishEnabled ? <LocaleSwitch className="max-md:hidden" /> : null}
          <CartButton />
        </div>
      </div>
    </header>
  );
}
