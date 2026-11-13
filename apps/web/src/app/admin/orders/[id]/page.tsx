import type { Metadata } from "next";
import { AdminOrderDetail } from "@/components/admin-order-detail";

export const metadata: Metadata = { title: "Order detail | RIGX Admin", robots: { index: false, follow: false } };

export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <AdminOrderDetail id={id} />;
}
