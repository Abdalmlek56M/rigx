import type { Metadata } from "next";
import { SecurityAuditAdmin } from "@/components/content-audit-admin";

export const metadata: Metadata = { title: "Audit log | RIGX Admin", robots: { index: false, follow: false } };

export default function AdminAuditPage() {
  return <SecurityAuditAdmin />;
}
