import type { Metadata } from "next";
import { PcBuilderCatalog } from "@/components/pc-builder-catalog";

export const metadata: Metadata = {
  title: "مجمّع الحاسب | RIGX",
  description: "اختر مكونات جهازك من قوالب RIGX المنشورة.",
  alternates: { canonical: "/pc-builder" },
};

export default function PcBuilderPage() {
  return <PcBuilderCatalog />;
}
