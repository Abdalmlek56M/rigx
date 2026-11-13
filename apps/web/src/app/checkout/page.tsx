import type { Metadata } from "next";
import { CheckoutExperience } from "@/components/checkout-experience";

export const metadata: Metadata = {
  title: "إتمام الطلب | RIGX",
  description: "أكمل بيانات التواصل والتسليم لتأكيد طلبك مع RIGX.",
  robots: { index: false, follow: false },
};

export default function Page() {
  return <CheckoutExperience />;
}
