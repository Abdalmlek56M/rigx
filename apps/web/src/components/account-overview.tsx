"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import {
  getCustomerAddresses,
  getIdentitySessions,
  getMyReviews,
  type CustomerAddress,
  type IdentitySession,
  type MyReviews,
} from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { useWishlist } from "@/components/wishlist-provider";
import { ProfileAvatar } from "@/components/profile-avatar";
import { AccountShell } from "@/components/account-shell";
import { LoadingCard, Message, PageIntro, SecondaryButton } from "@/components/ui";

export function AccountOverview() {
  const { messages } = useLocale();
  const { status, user } = useSession();
  const { count: wishlistCount } = useWishlist();
  const [addresses, setAddresses] = useState<CustomerAddress[]>([]);
  const [sessions, setSessions] = useState<IdentitySession[]>([]);
  const [reviews, setReviews] = useState<MyReviews | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (status !== "authenticated") return;
    let active = true;
    void Promise.all([
      getCustomerAddresses().catch(() => [] as CustomerAddress[]),
      getIdentitySessions().catch(() => [] as IdentitySession[]),
      getMyReviews().catch(() => null),
    ]).then(([addressesResult, sessionsResult, reviewsResult]) => {
      if (!active) return;
      setAddresses(addressesResult);
      setSessions(sessionsResult);
      setReviews(reviewsResult);
      setError(null);
    });
    return () => {
      active = false;
    };
  }, [status]);

  if (status === "loading") return <LoadingCard />;
  if (status === "unavailable") {
    return <main className="mx-auto w-full max-w-4xl px-5 py-10"><Message tone="error">{messages.accountUnavailable}</Message></main>;
  }
  if (!user) {
    return (
      <main className="mx-auto w-full max-w-4xl px-5 py-10">
        <Message tone="info">{messages.accountRedirecting}</Message>
        <Link href="/login" className="mt-3 inline-flex font-semibold text-[var(--ds-accent)]">{messages.navLogin}</Link>
      </main>
    );
  }

  const reviewCount = (reviews?.productReviews.length ?? 0) + (reviews?.storeReview.length ?? 0);
  const cards = [
    {
      href: "/account/security",
      title: messages.accountSecuritySection,
      body: messages.changePasswordLead,
      meta: `${sessions.length} ${messages.accountSessionsTitle}`,
    },
    {
      href: "/account/addresses",
      title: messages.accountAddressesSection,
      body: messages.addressLead,
      meta: messages.accountAddressesCount.replace("{count}", String(addresses.length)),
    },
    {
      href: "/account/wishlist",
      title: messages.accountWishlistSection,
      body: messages.wishlistLead,
      meta: messages.accountWishlistCount.replace("{count}", String(wishlistCount)),
    },
    {
      href: "/account/reviews",
      title: messages.accountReviewsSection,
      body: messages.reviewsMyLead,
      meta: messages.accountReviewsCount.replace("{count}", String(reviewCount)),
    },
    {
      href: "/account/profile",
      title: messages.accountPersonalInfo,
      body: messages.accountProfileLead,
      meta: user.email,
    },
  ];

  return (
    <AccountShell>
      <PageIntro eyebrow="RIGX / ACCOUNT" title={messages.accountOverviewTitle} lead={messages.accountOverviewLead} />
      {error ? <Message tone="error">{error}</Message> : null}

      <section className="rigx-admin-card grid gap-5 p-5 sm:grid-cols-[auto_1fr] sm:items-center">
        <ProfileAvatar size="lg" />
        <div className="grid gap-1">
          <h2 className="text-2xl font-black text-[var(--ds-text)]">{user.fullName || user.username}</h2>
          <p className="text-sm text-[var(--ds-text-muted)]" dir="ltr">@{user.username}</p>
          <p className="text-sm text-[var(--ds-text-muted)]" dir="ltr">{user.email}</p>
          <p className={`text-xs font-bold ${user.isEmailVerified ? "text-emerald-600" : "text-amber-600"}`}>
            {user.isEmailVerified ? messages.adminIdentityVerified : messages.adminIdentityNotVerified}
          </p>
          <p className="text-sm text-[var(--ds-text-muted)]" dir="ltr">{user.phoneNumber ?? "-"}</p>
          <SecondaryButton type="button" className="mt-2 w-fit" onClick={() => { window.location.href = "/account/profile"; }}>
            {messages.adminEdit}
          </SecondaryButton>
        </div>
      </section>

      <section className="grid gap-4 sm:grid-cols-2">
        {cards.map((card) => (
          <Link key={card.href} href={card.href} className="rigx-admin-card grid gap-2 p-5 transition hover:border-[var(--ds-accent)]">
            <p className="text-lg font-bold text-[var(--ds-text)]">{card.title}</p>
            <p className="text-sm leading-6 text-[var(--ds-text-muted)]">{card.body}</p>
            <p className="text-xs font-semibold text-[var(--ds-accent-strong)]">{card.meta}</p>
          </Link>
        ))}
      </section>
    </AccountShell>
  );
}

