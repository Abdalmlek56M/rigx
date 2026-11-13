import type { Metadata } from "next";
import { AccountOrderDetail } from "@/components/account-order-detail";

export const metadata: Metadata = { title: "Order details | RIGX", robots: { index: false, follow: false } };

export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <AccountOrderDetail id={id} />;
}
