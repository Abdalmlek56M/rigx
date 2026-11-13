import type { Metadata } from "next";
import { FinanceReportsAdmin } from "@/components/finance-reports-admin";

export const metadata: Metadata = { title: "Finance & reports | RIGX Admin", robots: { index: false, follow: false } };

export default function Page() {
  return <FinanceReportsAdmin />;
}
