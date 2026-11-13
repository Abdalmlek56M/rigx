import type { Metadata } from "next";
import { ContentFaqAdmin } from "@/components/content-faq-admin";

export const metadata: Metadata = { title: "FAQ | RIGX Admin", robots: { index: false, follow: false } };

export default function AdminFaqPage() {
  return <ContentFaqAdmin />;
}
