import type { Metadata } from "next";
import { ContentPoliciesAdmin } from "@/components/content-policies-admin";

export const metadata: Metadata = { title: "Policies | RIGX Admin", robots: { index: false, follow: false } };

export default function AdminPoliciesPage() {
  return <ContentPoliciesAdmin />;
}
