import { getTranslations } from "next-intl/server";
import { Platform, contactLinkProps, displayPhone, findLink } from "@/lib/contact";
import type { StorefrontData } from "@/lib/storefront";

/** Slim graphite strip above the header: the announcement and the WhatsApp number. */
export async function TopBar({ storefront }: { storefront: StorefrontData }) {
  const t = await getTranslations("topBar");
  const whatsApp = findLink(storefront.contactLinks, Platform.WhatsApp);

  return (
    <div className="bg-chrome text-[13px] text-footer-text">
      <div className="shell flex flex-wrap items-center justify-between gap-x-6 gap-y-1 py-2">
        <span className="flex items-center gap-2">
          <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-orange" />
          {storefront.announcementText ?? t("defaultAnnouncement")}
        </span>
        {whatsApp ? (
          <a {...contactLinkProps(whatsApp)} className="max-sm:hidden transition-colors hover:text-on-carbon">
            {t("whatsApp")} <span className="ltr-text">{displayPhone(whatsApp.value)}</span>
          </a>
        ) : null}
      </div>
    </div>
  );
}
