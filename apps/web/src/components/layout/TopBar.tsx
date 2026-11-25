import { getLocale, getTranslations } from "next-intl/server";
import { WhatsAppIcon } from "@/components/ui/icons";
import { Platform, contactLinkProps, findLink } from "@/lib/contact";
import { formatEgp } from "@/lib/format";
import type { StorefrontData } from "@/lib/storefront";
import { LocaleSwitch } from "./LocaleSwitch";
import { STRIP_BUTTON } from "./nav-links";
import { ThemeToggle } from "./ThemeToggle";
import { TopBarTicker } from "./TopBarTicker";

/**
 * Slim graphite strip above the header: the store's announcement and what it offers, one message at a time, on the start side;
 * on the end side WhatsApp (with its name from `sm`) and, from `lg`, the language and theme switches (in the menu on smaller screens).
 */
export async function TopBar({ storefront }: { storefront: StorefrontData }) {
  const t = await getTranslations("topBar");
  const locale = await getLocale();
  const freeShipping = storefront.checkout.freeShippingMinimum;
  const whatsApp = findLink(storefront.contactLinks, Platform.WhatsApp);
  const messages = [
    storefront.announcementText ?? t("defaultAnnouncement"),
    ...(storefront.whatsAppHours ? [t("whatsAppHours", { hours: storefront.whatsAppHours })] : []),
    ...(freeShipping !== null ? [t("freeShipping", { amount: formatEgp(freeShipping, locale) })] : []),
    ...(storefront.pickupEnabled ? [t("pickup")] : [])
  ];

  return (
    <div className="bg-chrome text-[13px] text-footer-text">
      <div className="shell flex min-h-9 items-center justify-between gap-4">
        <TopBarTicker messages={messages} />
        <div className="flex shrink-0 items-center gap-0.5">
          {whatsApp ? (
            <a {...contactLinkProps(whatsApp)} aria-label={t("whatsAppAria")} className={STRIP_BUTTON}>
              <WhatsAppIcon size={15} />
              <span className="hidden sm:inline">{t("whatsApp")}</span>
            </a>
          ) : null}
          {storefront.englishEnabled ? (
            <>
              <span aria-hidden="true" className="mx-1 hidden h-4 w-px bg-white/15 lg:block" />
              <LocaleSwitch className="hidden lg:flex" />
            </>
          ) : null}
          <span aria-hidden="true" className="mx-1 hidden h-4 w-px bg-white/15 lg:block" />
          <ThemeToggle strip className="hidden lg:flex" />
        </div>
      </div>
    </div>
  );
}
