import type { Metadata } from "next";
import { OffersExperience } from "@/components/offers-experience";

export const metadata: Metadata = {
  title: "العروض | RIGX",
  description: "العروض المنشورة حاليًا من RIGX.",
  alternates: { canonical: "/offers" },
};

export default function OffersPage() {
  return <OffersExperience />;
}
