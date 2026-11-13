import type { Metadata } from "next";
import { ContactExperience } from "@/components/contact-experience";

export const metadata: Metadata = {
  title: "تواصل معنا | RIGX",
  description: "تواصل مع فريق RIGX للاستفسارات الفنية والتوافق وتوفير القطع ومتابعة الطلبات.",
  alternates: { canonical: "/contact" },
};

export default function ContactPage() {
  return <ContactExperience />;
}
