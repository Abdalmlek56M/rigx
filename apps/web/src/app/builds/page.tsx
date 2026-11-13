import type { Metadata } from "next";
import { BuildsGallery } from "@/components/builds-gallery";

export const metadata: Metadata = {
  title: "RIGX builds | RIGX",
  description: "Published RIGX PC builds assembled from available inventory units.",
};

export default function BuildsPage() {
  return <BuildsGallery />;
}
