import type { Metadata } from "next";
import { PcBuilderAdmin } from "@/components/pc-builder-admin";

export const metadata: Metadata = { title: "PC Builder | RIGX Admin", robots: { index: false, follow: false } };

export default function AdminPcBuilderPage() {
  return <PcBuilderAdmin />;
}
