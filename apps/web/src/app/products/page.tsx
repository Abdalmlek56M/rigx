import type { Metadata } from "next";
import { Suspense } from "react";
import { RigxCatalog } from "@/components/rigx-catalog";
import { LoadingCard } from "@/components/ui";

export const metadata: Metadata = {
  title: "المنتجات | RIGX",
  description: "تصفح منتجات الكمبيوتر المنشورة في RIGX مع البحث والفلاتر والمواصفات والأسعار.",
  alternates: { canonical: "/products" },
  openGraph: {
    title: "المنتجات | RIGX",
    description: "تصفح منتجات الكمبيوتر المنشورة مع مواصفات وأسعار الكتالوج.",
    url: "/products",
    siteName: "RIGX",
    type: "website",
  },
};

export default function ProductsPage() {
  return (
    <Suspense fallback={<LoadingCard />}>
      <RigxCatalog />
    </Suspense>
  );
}

