import { getTranslations } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import { Logo } from "@/components/ui/Logo";
import { MailIcon, PhoneIcon, WhatsAppIcon } from "@/components/ui/icons";
import { publishedPolicySlugs } from "@/features/policies/published-policies";
import { Platform, SOCIAL_PLATFORMS, contactLinkProps, displayPhone, distinctPhone, findLink } from "@/lib/contact";
import type { StorefrontData } from "@/lib/storefront";
import { PlatformIcon } from "./PlatformIcon";

const linkClass = "flex min-h-11 items-center text-sm text-footer-text transition-colors hover:text-on-carbon md:min-h-9";
const headingClass = "m-0 text-[13px] font-bold uppercase tracking-wide text-on-carbon";
const contactRow = "group flex min-h-11 items-start gap-3 text-sm text-footer-text transition-colors hover:text-on-carbon";
const contactIcon = "flex h-9 w-9 shrink-0 items-center justify-center rounded-full border border-footer-line text-on-carbon transition-colors group-hover:border-orange group-hover:text-orange";
const contactLabel = "text-[12px] leading-5 text-footer-text";
const contactValue = "block min-w-0 truncate text-sm font-semibold text-on-carbon";

/** Dark footer (both themes): brand, shop links, help (only published policies + FAQ), contact label/value rows. */
export async function Footer({ storefront }: { storefront: StorefrontData }) {
  const [t, tp, policies] = await Promise.all([getTranslations("footer"), getTranslations("policies.kinds"), publishedPolicySlugs()]);
  // WhatsApp has its own row under "تواصل معنا", so the social icons skip it (one number, one entry).
  const social = storefront.contactLinks.filter(
    (l) => l.showOnSite && l.platform !== Platform.WhatsApp && (SOCIAL_PLATFORMS as readonly string[]).includes(l.platform)
  );
  const whatsApp = findLink(storefront.contactLinks, Platform.WhatsApp);
  const phone = distinctPhone(storefront.contactLinks);

  const shop = [
    { href: "/products", label: t("allProducts") },
    { href: "/products?condition=New", label: t("shop.new") },
    { href: "/products?condition=Used", label: t("shop.used") },
    { href: "/builds", label: t("shop.builds") },
    { href: "/customer-builds", label: t("shop.customerBuilds") },
    { href: "/builder", label: t("shop.builder") }
  ];
  const help = [
    ...policies.map((slug) => ({ href: `/policies/${slug}`, label: tp(slug) })),
    { href: "/warranty", label: t("warrantyLookup") },
    { href: "/faq", label: t("faq") }
  ];

  return (
    <footer className="mt-16 bg-chrome text-footer-text">
      <div className="shell grid grid-cols-2 gap-x-6 gap-y-10 pb-10 pt-14 lg:grid-cols-[1.5fr_1fr_1fr_1.3fr] lg:gap-12">
        <div className="col-span-2 flex flex-col gap-4 lg:col-span-1">
          <Link href="/" aria-label="RIGX" className="flex self-start text-on-carbon">
            <Logo width={132} />
          </Link>
          <p className="m-0 max-w-[32ch] text-sm leading-[1.8]">{t("tagline")}</p>
          {social.length > 0 ? (
            <div className="flex flex-wrap gap-2 pt-1">
              {social.map((link) => (
                <a
                  key={link.platform}
                  {...contactLinkProps(link)}
                  aria-label={t(`platforms.${link.platform as (typeof SOCIAL_PLATFORMS)[number]}`)}
                  className="press flex h-11 w-11 items-center justify-center rounded-full border border-footer-line text-on-carbon transition-colors hover:border-orange hover:text-orange"
                >
                  <PlatformIcon platform={link.platform} />
                </a>
              ))}
            </div>
          ) : null}
        </div>

        <nav aria-labelledby="footer-shop" className="flex flex-col gap-1">
          <h2 id="footer-shop" className={`${headingClass} mb-2`}>{t("shop.title")}</h2>
          {shop.map((item) => (
            <Link key={item.href} href={item.href} className={linkClass}>{item.label}</Link>
          ))}
        </nav>

        <nav aria-labelledby="footer-help" className="flex flex-col gap-1">
          <h2 id="footer-help" className={`${headingClass} mb-2`}>{t("help")}</h2>
          {help.map((item) => (
            <Link key={item.href} href={item.href} className={linkClass}>{item.label}</Link>
          ))}
        </nav>

        <div className="col-span-2 flex flex-col gap-1 lg:col-span-1">
          <h2 className={`${headingClass} mb-2`}>{t("contact")}</h2>
          <dl className="m-0 flex flex-col gap-1">
            {whatsApp ? (
              <div>
                <dt className="sr-only">{t("platforms.WhatsApp")}</dt>
                <dd className="m-0">
                  <a {...contactLinkProps(whatsApp)} className={contactRow}>
                    <span className={contactIcon}><WhatsAppIcon size={18} /></span>
                    <span className="flex min-w-0 flex-col">
                      <span className={contactLabel}>{t("platforms.WhatsApp")}</span>
                      <span dir="ltr" className={`${contactValue} text-start font-mono`}>{displayPhone(whatsApp.value)}</span>
                    </span>
                  </a>
                </dd>
              </div>
            ) : null}
            {phone ? (
              <div>
                <dt className="sr-only">{t("phone")}</dt>
                <dd className="m-0">
                  <a {...contactLinkProps(phone)} className={contactRow}>
                    <span className={contactIcon}><PhoneIcon size={18} /></span>
                    <span className="flex min-w-0 flex-col">
                      <span className={contactLabel}>{t("phone")}</span>
                      <span dir="ltr" className={`${contactValue} text-start font-mono`}>{displayPhone(phone.value)}</span>
                    </span>
                  </a>
                </dd>
              </div>
            ) : null}
            {storefront.email ? (
              <div>
                <dt className="sr-only">{t("email")}</dt>
                <dd className="m-0">
                  <a href={`mailto:${storefront.email}`} className={contactRow}>
                    <span className={contactIcon}><MailIcon size={18} /></span>
                    <span className="flex min-w-0 flex-col">
                      <span className={contactLabel}>{t("email")}</span>
                      <span dir="ltr" title={storefront.email} className={`${contactValue} text-start [overflow-wrap:normal]`}>{storefront.email}</span>
                    </span>
                  </a>
                </dd>
              </div>
            ) : null}
          </dl>
          {storefront.whatsAppHours ? <p className="m-0 mt-2 text-[13px] leading-[1.7]">{storefront.whatsAppHours}</p> : null}
          {storefront.pickupAddress ? <p className="m-0 text-[13px] leading-[1.7]">{storefront.pickupAddress}</p> : null}
        </div>
      </div>
      <div className="shell">
        <div className="flex flex-wrap items-center justify-between gap-2 border-t border-footer-line pb-28 pt-5 text-xs md:pb-24">
          <span>{t("copyright", { year: new Date().getFullYear() })}</span>
          {storefront.domain ? <span className="ltr-text text-[13px]">{storefront.domain}</span> : null}
        </div>
      </div>
    </footer>
  );
}
