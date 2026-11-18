import { getTranslations } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { Logo } from "@/components/ui/Logo";
import { MailIcon, PhoneIcon, WhatsAppIcon } from "@/components/ui/icons";
import { Platform, SOCIAL_PLATFORMS, contactHref, displayPhone, findLink } from "@/lib/contact";
import type { StorefrontData } from "@/lib/storefront";
import { PlatformIcon } from "./PlatformIcon";

const linkClass = "flex min-h-11 items-center text-sm text-footer-text hover:text-on-carbon md:min-h-0";
const headingClass = "font-bold text-on-carbon";
const contactClass = "flex items-center gap-2.5 text-on-carbon";

export async function Footer({ storefront }: { storefront: StorefrontData }) {
  const t = await getTranslations("footer");
  const social = storefront.contactLinks.filter((l) => l.showOnSite && (SOCIAL_PLATFORMS as readonly string[]).includes(l.platform));
  const whatsApp = findLink(storefront.contactLinks, Platform.WhatsApp);
  const phone = findLink(storefront.contactLinks, Platform.Phone);

  const shop = [
    { href: "/products", label: t("allProducts") },
    { href: "/products?condition=New", label: t("shop.new") },
    { href: "/products?condition=Used", label: t("shop.used") },
    { href: "/builds", label: t("shop.builds") },
    { href: "/builder", label: t("shop.builder") }
  ];
  const help = [
    { href: "/policies/warranty", label: t("warranty") },
    { href: "/policies/returns", label: t("returns") },
    { href: "/policies/shipping", label: t("shipping") },
    { href: "/about", label: t("about") },
    { href: "/articles", label: t("articles") }
  ];

  return (
    <footer className="bg-carbon text-footer-text">
      <div className="mx-auto grid max-w-[1240px] gap-8 px-6 pb-7 pt-14 md:grid-cols-[1.4fr_1fr_1fr_1.2fr]">
        <div className="flex flex-col gap-4">
          <Logo width={140} />
          <span className="text-sm leading-[1.8]">{t("tagline")}</span>
          {social.length > 0 ? (
            <div className="flex flex-col gap-2.5">
              <span className={headingClass}>{t("follow")}</span>
              <div className="flex flex-wrap gap-2">
                {social.map((link) => (
                  <a
                    key={link.platform}
                    href={contactHref(link)}
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label={t(`platforms.${link.platform as (typeof SOCIAL_PLATFORMS)[number]}`)}
                    className="flex h-11 w-11 items-center justify-center rounded-card border border-header-line text-on-carbon"
                  >
                    <PlatformIcon platform={link.platform} />
                  </a>
                ))}
              </div>
            </div>
          ) : null}
        </div>

        <nav aria-label={t("shop.title")} className="flex flex-col gap-2.5">
          <span className={headingClass}>{t("shop.title")}</span>
          {shop.map((item) => (
            <Link key={item.href} href={item.href} className={linkClass}>{item.label}</Link>
          ))}
        </nav>

        <nav aria-label={t("help")} className="flex flex-col gap-2.5">
          <span className={headingClass}>{t("help")}</span>
          {help.map((item) => (
            <Link key={item.href} href={item.href} className={linkClass}>{item.label}</Link>
          ))}
        </nav>

        <div className="flex flex-col gap-3">
          <span className={headingClass}>{t("contact")}</span>
          {whatsApp ? (
            <a href={contactHref(whatsApp)} className={`${contactClass} min-h-11`}>
              <WhatsAppIcon />
              <span className="ltr-text text-[15px]">{displayPhone(whatsApp.value)}</span>
            </a>
          ) : null}
          {phone ? (
            <a href={contactHref(phone)} className={`${contactClass} min-h-11`}>
              <PhoneIcon />
              <span className="ltr-text text-[15px]">{displayPhone(phone.value)}</span>
            </a>
          ) : null}
          {storefront.email ? (
            <a href={`mailto:${storefront.email}`} className={`${contactClass} min-h-11 text-sm`}>
              <MailIcon />
              <span className="ltr-text text-[13px]">{storefront.email}</span>
            </a>
          ) : null}
          {storefront.whatsAppHours ? <span className="text-[13px] leading-[1.7]">{storefront.whatsAppHours}</span> : null}
          {storefront.pickupAddress ? <span className="text-[13px] leading-[1.7]">{storefront.pickupAddress}</span> : null}
          <Link
            href="/support"
            className="inline-flex h-11 items-center self-start rounded-control border-[1.5px] border-on-carbon px-4 text-sm font-bold text-on-carbon"
          >
            {t("support")}
          </Link>
        </div>
      </div>
      <div className="mx-auto flex max-w-[1240px] flex-wrap justify-between gap-2 border-t border-footer-line px-6 py-[18px] text-xs text-footer-text">
        <span>{t("copyright", { year: new Date().getFullYear() })}</span>
        {storefront.domain ? <span className="ltr-text text-[13px]">{storefront.domain}</span> : null}
      </div>
    </footer>
  );
}
