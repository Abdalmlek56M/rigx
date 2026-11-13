"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useLocale } from "@/lib/i18n";
import { Icon, type IconName } from "@/components/icons";

type AccountLink = { href: string; label: string; icon: IconName };

export function AccountNav() {
  const { messages } = useLocale();
  const pathname = usePathname() ?? "";
  const links: AccountLink[] = [
    { href: "/account", label: messages.accountOverviewTitle, icon: "dashboard" },
    { href: "/account/profile", label: messages.accountPersonalInfo, icon: "user" },
    { href: "/account/orders", label: messages.accountOrdersSection, icon: "receipt" },
    { href: "/account/wishlist", label: messages.accountWishlistSection, icon: "heart" },
    { href: "/account/reviews", label: messages.accountReviewsSection, icon: "sparkles" },
    { href: "/account/addresses", label: messages.accountAddressesSection, icon: "store" },
    { href: "/account/security", label: messages.accountSecuritySection, icon: "check" },
    { href: "/account/sessions", label: messages.accountSessionsSection, icon: "eye" },
    { href: "/account/notifications", label: messages.notificationsTitle, icon: "receipt" },
  ];
  return (
    <aside className="account-nav glass-panel" aria-label={messages.accountOverviewTitle}>
      <span className="eyebrow">RIGX / ACCOUNT</span>
      {links.map((link) => {
        const active = pathname === link.href;
        return (
          <Link
            key={link.href}
            href={link.href}
            aria-current={active ? "page" : undefined}
            className={`account-tab${active ? " active" : ""}`}
          >
            <Icon name={link.icon} />
            <span>{link.label}</span>
          </Link>
        );
      })}
    </aside>
  );
}

export function AccountBackLink() {
  const { messages } = useLocale();
  return (
    <Link href="/account" className="text-link">
      ‹ {messages.accountBackToOverview}
    </Link>
  );
}
