import type { Metadata } from "next";
import { RetailStockAdmin } from "@/components/retail-stock-admin";
export const metadata: Metadata = { title: "Stock | RIGX Admin", robots: { index: false, follow: false } };
export default function Page() { return <RetailStockAdmin />; }
