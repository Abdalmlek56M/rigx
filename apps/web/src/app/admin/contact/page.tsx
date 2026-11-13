import type { Metadata } from "next";
import { ContactInboxAdmin } from "@/components/contact-inbox-admin";

export const metadata: Metadata = { title: "Contact inbox | RIGX Admin", robots: { index: false, follow: false } };

export default function Page() {
  return <ContactInboxAdmin />;
}
