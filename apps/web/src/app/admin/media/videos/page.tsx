import type { Metadata } from "next";
import { ContentVideosAdmin } from "@/components/content-videos-admin";

export const metadata: Metadata = { title: "Videos | RIGX Admin", robots: { index: false, follow: false } };

export default function AdminVideosPage() {
  return <ContentVideosAdmin />;
}
