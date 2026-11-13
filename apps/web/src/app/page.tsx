import type { Metadata } from "next";
import { RigxHome } from "@/components/rigx-home";

export const metadata: Metadata = {
  title: "RIGX | PC Components & Hardware",
  description: "Browse PC components, graphics cards, processors, memory, storage, and peripherals with clear catalog data.",
  alternates: { canonical: "/" },
  openGraph: {
    title: "RIGX | PC Components & Hardware",
    description: "Technology-focused PC hardware retail with clear specifications and catalog pricing.",
    type: "website",
    url: "/",
    siteName: "RIGX",
  },
};

export default function HomePage() {
  return <RigxHome />;
}
