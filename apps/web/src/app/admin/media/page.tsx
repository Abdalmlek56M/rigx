import type { Metadata } from "next";
import { ContentMediaAdmin } from "@/components/content-media-admin";

export const metadata: Metadata = { title: "Media library | RIGX Admin", robots: { index: false, follow: false } };

export default function AdminMediaPage() {
  return <ContentMediaAdmin />;
}
