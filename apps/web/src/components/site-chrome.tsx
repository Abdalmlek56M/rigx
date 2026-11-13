"use client";

import { useLocale } from "@/lib/i18n";
import { localizedCategoryName } from "@/lib/category-labels";
import { useStoreSettings } from "@/components/store-settings-provider";
import { Icon, type IconName } from "@/components/icons";
import Link from "next/link";

export function SkipToContent() {
  const { messages } = useLocale();
  return (
    <a
      href="#main-content"
      className="sr-only focus:not-sr-only focus:fixed focus:start-4 focus:top-4 focus:z-50 focus:rounded-lg focus:bg-[var(--ds-accent)] focus:px-4 focus:py-2 focus:text-[var(--ds-on-accent)]"
    >
      {messages.skipToContent}
    </a>
  );
}

const departmentLinks: Array<[string, string]> = [
  ["graphics-cards", "products?category=graphics-cards"],
  ["processors", "products?category=processors"],
  ["motherboards", "products?category=motherboards"],
  ["memory", "products?category=memory"],
  ["storage", "products?category=storage"],
  ["power-supplies", "products?category=power-supplies"],
];

export function SiteFooter() {
  const { messages, locale } = useLocale();
  const { settings } = useStoreSettings();
  const isPreview = process.env.NEXT_PUBLIC_PREVIEW_MODE === "true";
  const socialLinks: Array<{ label: string; href: string; icon: IconName }> = [
    { label: messages.facebook, href: settings?.facebookUrl ?? "", icon: "facebook" as const },
    { label: messages.telegram, href: settings?.telegramUrl ?? "", icon: "telegram" as const },
    { label: messages.tiktok, href: settings?.tikTokUrl ?? "", icon: "tiktok" as const },
    { label: messages.youtube, href: settings?.youTubeUrl ?? "", icon: "youtube" as const },
    { label: messages.instagram, href: settings?.instagramUrl ?? "", icon: "instagram" as const },
  ].filter((item) => item.href.length > 0);

  return (
    <footer className="rigx-site-footer">
      <div className="rigx-footer-inner">
        <div className="rigx-footer-brand">
          <Link href="/" className="rigx-footer-logo" aria-label="RIGX">
            RIG<span>X</span>
          </Link>
          <p>{messages.footerStorefrontDescription}</p>
          {socialLinks.length > 0 ? (
            <div className="rigx-footer-social">
              <span>{messages.footerFollowUs}</span>
              <div>
                {socialLinks.map((item) => (
                  <a
                    key={item.icon}
                    href={item.href}
                    target="_blank"
                    rel="noreferrer"
                    aria-label={item.label}
                    title={item.label}
                  >
                    <Icon name={item.icon} className="h-4 w-4" />
                  </a>
                ))}
              </div>
            </div>
          ) : null}
        </div>

        <FooterLinks
          title={messages.footerDepartments}
          links={departmentLinks.map(([slug]) => [
            localizedCategoryName(slug, slug, locale),
            `/products?category=${encodeURIComponent(slug)}`,
          ])}
        />

        <FooterLinks
          title={messages.footerHelpInfo}
          links={[
            [messages.footerAllProducts, "/products"],
            [messages.footerAllDepartments, "/categories"],
            [messages.footerCartLink, "/cart"],
          ]}
        />

        <div className="rigx-footer-system">
          <strong>RIGX</strong>
          <p>{messages.footerSystemNote}</p>
          <Link href="/products" className="rigx-footer-top">
            {messages.footerAllProducts}
            <span aria-hidden="true">←</span>
          </Link>
        </div>
      </div>
      <div className="rigx-footer-bottom">
        <p>{messages.footerRights}</p>
        {isPreview ? <span className="rigx-footer-demo">{messages.footerDemoBadge}</span> : null}
      </div>
    </footer>
  );
}

function FooterLinks({ title, links }: { title: string; links: Array<[string, string]> }) {
  return (
    <div className="rigx-footer-links">
      <h2>{title}</h2>
      <ul>
        {links.map(([label, href]) => (
          <li key={`${href}-${label}`}>
            <Link href={href}>{label}</Link>
          </li>
        ))}
      </ul>
    </div>
  );
}
