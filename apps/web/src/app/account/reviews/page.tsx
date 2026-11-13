import type { Metadata } from "next";
import { AccountReviews } from "@/components/account-reviews";

export const metadata: Metadata = { title: "My reviews | RIGX", robots: { index: false, follow: false } };

export default function AccountReviewsPage() {
  return <AccountReviews />;
}
