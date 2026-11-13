import type { Metadata } from "next";
import { RetailOrdersAdmin } from "@/components/retail-orders-admin";
export const metadata: Metadata = { title: "Orders | RIGX Admin", robots: { index: false, follow: false } };
export default function Page() { return <RetailOrdersAdmin />; }
