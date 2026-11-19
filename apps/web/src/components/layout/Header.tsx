import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { UserIcon } from "@/components/ui/icons";
import { Logo } from "@/components/ui/Logo";
import { AccountLink } from "@/features/auth/AccountLink";
import { CartButton } from "@/features/cart/CartButton";
import type { StorefrontData } from "@/lib/storefront";
import { CategoriesMenu } from "./CategoriesMenu";
import { LocaleSwitch } from "./LocaleSwitch";
import { MobileMenu } from "./MobileMenu";
import { HEADER_ICON_BUTTON, NAV_LINKS } from "./nav-links";
import { SearchForm } from "./SearchForm";
import { ThemeToggle } from "./ThemeToggle";

/** Sticky light header: logo, navigation, a wide search and the icon controls; the search drops to its own row on phones. */
export function Header({ storefront }: { storefront: StorefrontData }) {
  const t = useTranslations("header");

  return (
    <header className="site-header sticky top-0 z-40 border-b border-line bg-surface text-ink">
      <div className="shell flex flex-wrap items-center gap-x-6 gap-y-2.5 py-2.5 md:flex-nowrap md:py-3">
        <MobileMenu categories={storefront.categories} englishEnabled={storefront.englishEnabled} />
        <Link href="/" aria-label={t("home")} className="press flex text-ink">
          <Logo width={112} />
        </Link>
        <nav aria-label={t("navLabel")} className="hidden items-center gap-x-1 lg:flex">
          <CategoriesMenu categories={storefront.categories} />
          {NAV_LINKS.map((link) => (
            <Link
              key={link.key}
              href={link.href}
              className={`nav-link flex min-h-11 items-center whitespace-nowrap rounded-full px-3 text-[15px] font-medium transition-colors hover:bg-paper ${link.emphasis ? "text-orange-text" : ""}`}
            >
              {t(link.key)}
            </Link>
          ))}
        </nav>
        <div className="order-last w-full md:order-none md:ms-auto md:w-auto md:min-w-[240px] md:max-w-[440px] md:flex-1">
          <SearchForm />
        </div>
        <div className="ms-auto flex items-center gap-1 md:ms-0">
          <AccountLink ariaLabel={t("account")} className={`${HEADER_ICON_BUTTON} max-md:hidden`}>
            <UserIcon />
          </AccountLink>
          <ThemeToggle className="max-md:hidden" />
          {storefront.englishEnabled ? <LocaleSwitch className="max-md:hidden" /> : null}
          <CartButton />
        </div>
      </div>
    </header>
  );
}
