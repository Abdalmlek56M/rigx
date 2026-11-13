import type { Metadata } from "next";
import { ContentReviewsAdmin } from "@/components/content-reviews-admin";

export const metadata: Metadata = { title: "Review moderation | RIGX Admin", robots: { index: false, follow: false } };

export default function AdminReviewsPage() {
  return <ContentReviewsAdmin />;
}
