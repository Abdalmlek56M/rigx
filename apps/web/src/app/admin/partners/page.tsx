import type { Metadata } from "next";
import { RetailPartnerAdmin } from "@/components/retail-partner-admin";
export const metadata: Metadata = { title: "Partners | RIGX Admin", robots: { index: false, follow: false } };
export default function Page() { return <RetailPartnerAdmin />; }
