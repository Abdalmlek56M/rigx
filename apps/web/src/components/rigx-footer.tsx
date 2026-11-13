"use client";

import Link from "next/link";
import { useLocale } from "@/lib/i18n";
import { useStoreSettings } from "@/components/store-settings-provider";
import { localizedCategoryName } from "@/lib/category-labels";

const departments = [
  "graphics-cards",
  "processors",
  "motherboards",
  "memory",
  "storage",
  "power-supplies",
  "cooling",
  "cases",
];

export function RigxFooter() {
  const { locale, messages } = useLocale();
  const isArabic = locale === "ar";
  const { settings } = useStoreSettings();

  const socials = [
    { label: "Facebook", href: settings?.facebookUrl },
    { label: "Telegram", href: settings?.telegramUrl },
    { label: "TikTok", href: settings?.tikTokUrl },
    { label: "YouTube", href: settings?.youTubeUrl },
    { label: "Instagram", href: settings?.instagramUrl },
  ].filter((item): item is { label: string; href: string } => Boolean(item.href));

  return (
    <footer className="site-footer shell">
      <div className="footer-top">
        <div className="footer-brand">
          <span className="brand-mark">RIG<span>X</span></span>
          <p>
            {isArabic
              ? "متجر مكوّنات وأجهزة الكمبيوتر. كتالوج واضح، مخزون فعلي، وتجميعات تُبنى بوعي."
              : "PC components and systems. A clear catalog, real inventory, and builds made with intention."}
          </p>
          {socials.length > 0 ? (
            <div className="footer-social">
              {socials.map((social) => (
                <a key={social.label} href={social.href} target="_blank" rel="noreferrer">{social.label}</a>
              ))}
            </div>
          ) : null}
        </div>
        <div>
          <b>{isArabic ? "الأقسام" : "Departments"}</b>
          {departments.map((slug) => (
            <Link key={slug} href={`/products?category=${encodeURIComponent(slug)}`}>
              {localizedCategoryName(slug, slug, locale)}
            </Link>
          ))}
        </div>
        <div>
          <b>{isArabic ? "المتجر" : "Store"}</b>
          <Link href="/products">{isArabic ? "كل المنتجات" : "All products"}</Link>
          <Link href="/builds">{isArabic ? "التجميعات" : "Builds"}</Link>
          <Link href="/pc-builder">{isArabic ? "ابنِ جهازك" : "PC Builder"}</Link>
          <Link href="/offers">{isArabic ? "العروض" : "Offers"}</Link>
          <Link href="/articles">{isArabic ? "المجلة" : "Journal"}</Link>
          <Link href="/videos">{isArabic ? "الفيديوهات" : "Videos"}</Link>
        </div>
        <div>
          <b>{isArabic ? "المساعدة" : "Help"}</b>
          <Link href="/faq">{isArabic ? "الأسئلة الشائعة" : "FAQ"}</Link>
          <Link href="/contact">{isArabic ? "تواصل معنا" : "Contact"}</Link>
          <Link href="/policies/shipping">{isArabic ? "الشحن" : "Shipping"}</Link>
          <Link href="/policies/returns">{isArabic ? "الاستبدال والاسترجاع" : "Returns"}</Link>
          <Link href="/policies/warranty">{isArabic ? "الضمان" : "Warranty"}</Link>
          <Link href="/policies/terms">{isArabic ? "شروط البيع" : "Terms"}</Link>
          {settings?.pickupHours ? <span>{settings.pickupHours}</span> : null}
        </div>
      </div>
      <div className="footer-bottom">
        <span>RIGX HARDWARE © {new Date().getFullYear()}</span>
        <span>{isArabic ? "كل الحقوق محفوظة" : "All rights reserved"}</span>
        <Link href="/policies/privacy">{isArabic ? "الخصوصية" : "Privacy"}</Link>
        <span>{messages.footerSystemNote ?? "RIGX"}</span>
      </div>
    </footer>
  );
}
