import type { Metadata } from "next";
import { ContentOffersAdmin } from "@/components/content-offers-admin";

export const metadata: Metadata = { title: "Offers | RIGX Admin", robots: { index: false, follow: false } };

export default function AdminOffersPage() {
  return <ContentOffersAdmin />;
}
