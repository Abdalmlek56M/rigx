import { getTranslations } from "next-intl/server";
import { Platform, displayPhone, findLink } from "@/lib/contact";
import type { StorefrontData } from "@/lib/storefront";

export async function TopBar({ storefront }: { storefront: StorefrontData }) {
  const t = await getTranslations("topBar");
  const whatsApp = findLink(storefront.contactLinks, Platform.WhatsApp);

  return (
    <div className="bg-orange text-sm font-medium text-on-orange">
      <div className="mx-auto flex max-w-[1240px] flex-wrap justify-between gap-x-6 gap-y-1.5 px-6 py-2">
        <span>{storefront.announcementText ?? t("defaultAnnouncement")}</span>
        {whatsApp ? (
          <span>
            {t("whatsApp")} <span className="ltr-text">{displayPhone(whatsApp.value)}</span>
          </span>
        ) : null}
      </div>
    </div>
  );
}
