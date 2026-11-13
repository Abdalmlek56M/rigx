import type { Metadata } from "next";
import { AccountOrders } from "@/components/account-orders";

export const metadata: Metadata = { title: "My orders | RIGX", robots: { index: false, follow: false } };

export default function AccountOrdersPage() {
  return <AccountOrders />;
}
