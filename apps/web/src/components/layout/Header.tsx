import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { UserIcon } from "@/components/ui/icons";
import { Logo } from "@/components/ui/Logo";
import { AccountLink } from "@/features/auth/AccountLink";
import { CartButton } from "@/features/cart/CartButton";
import { NotificationBell } from "@/features/notifications/NotificationBell";
import type { StorefrontData } from "@/lib/storefront";
import { CategoriesMenu } from "./CategoriesMenu";
import { MobileMenu } from "./MobileMenu";
import { HEADER_ICON_BUTTON, HEADER_ICON_CLUSTER, NAV_LINKS } from "./nav-links";
import { SearchForm } from "./SearchForm";

/**
 * Sticky header: logo, the full navigation from xl, the search and the account and cart controls (with their names from xl;
 * theme and language live on the top strip). Between md and xl the navigation moves into the menu sheet. On phones the search
 * takes its own row.
 */
export function Header({ storefront }: { storefront: StorefrontData }) {
  const t = useTranslations("header");

  return (
    <header className="site-header sticky top-0 z-40 border-b border-line bg-surface text-ink">
      <div className="shell flex flex-wrap items-center gap-x-4 gap-y-2.5 py-2.5 md:min-h-[72px] md:flex-nowrap md:gap-x-6 md:py-3">
        <MobileMenu categories={storefront.categories} englishEnabled={storefront.englishEnabled} />
        <Link href="/" aria-label={t("home")} className="press flex shrink-0 text-ink">
          <Logo width={104} />
        </Link>
        <nav aria-label={t("navLabel")} className="hidden items-center gap-x-1 xl:flex">
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
        <div className="order-last w-full min-w-0 md:order-none md:ms-auto md:w-auto md:max-w-[460px] md:flex-1">
          <SearchForm />
        </div>
        <div className={`ms-auto shrink-0 md:ms-0 ${HEADER_ICON_CLUSTER}`}>
          <NotificationBell />
          <AccountLink ariaLabel={t("account")} className={`${HEADER_ICON_BUTTON} hidden sm:flex xl:px-3`}>
            <UserIcon />
            <span className="hidden text-sm font-bold xl:inline">{t("account")}</span>
          </AccountLink>
          <CartButton />
        </div>
      </div>
    </header>
  );
}
