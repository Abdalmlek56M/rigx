import type { Metadata } from "next";
import { FaqExperience } from "@/components/faq-experience";

export const metadata: Metadata = {
  title: "الأسئلة الشائعة | RIGX",
  description: "إجابات الأسئلة الشائعة المنشورة من RIGX.",
  alternates: { canonical: "/faq" },
  openGraph: {
    title: "الأسئلة الشائعة | RIGX",
    description: "إجابات الأسئلة الشائعة المنشورة من RIGX.",
    type: "website",
    url: "/faq",
    siteName: "RIGX",
  },
};

export default function FaqPage() {
  return <FaqExperience />;
}
