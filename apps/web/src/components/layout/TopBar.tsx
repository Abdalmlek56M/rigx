import { getLocale, getTranslations } from "next-intl/server";
import { WhatsAppIcon } from "@/components/ui/icons";
import { Platform, contactLinkProps, findLink } from "@/lib/contact";
import { formatEgp } from "@/lib/format";
import type { StorefrontData } from "@/lib/storefront";
import { TopBarTicker } from "./TopBarTicker";

/** Slim graphite strip above the header: the store's announcement and what it offers, one message at a time, and WhatsApp as an icon. */
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
      <div className="shell flex min-h-10 items-center justify-between gap-4 py-1">
        <TopBarTicker messages={messages} />
        {whatsApp ? (
          <a
            {...contactLinkProps(whatsApp)}
            aria-label={t("whatsAppAria")}
            data-tip={t("whatsAppAria")}
            className="tip flex size-8 shrink-0 items-center justify-center rounded-full transition-colors hover:bg-white/10 hover:text-on-carbon"
          >
            <WhatsAppIcon size={16} />
          </a>
        ) : null}
      </div>
    </div>
  );
}
