import type { Metadata } from "next";
import { PurchaseDraftsAdmin } from "@/components/purchase-drafts-admin";

export const metadata: Metadata = { title: "Purchase drafts | RIGX Admin", robots: { index: false, follow: false } };

export default function AdminPurchaseDraftsPage() {
  return <PurchaseDraftsAdmin />;
}
