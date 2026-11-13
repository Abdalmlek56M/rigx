import type { Metadata } from "next";
import { ContentArticlesAdmin } from "@/components/content-articles-admin";

export const metadata: Metadata = { title: "Articles | RIGX Admin", robots: { index: false, follow: false } };

export default function AdminArticlesPage() {
  return <ContentArticlesAdmin />;
}
