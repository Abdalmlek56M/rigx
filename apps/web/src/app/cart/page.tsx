import type { Metadata } from "next";
import { RigxCart } from "@/components/rigx-cart";

export const metadata: Metadata = {
  title: "سلة التسوق | RIGX",
  description: "راجع منتجات السلة والكميات قبل تجربة الشراء في RIGX.",
  robots: { index: false, follow: false },
};

export default function Page() {
  return <RigxCart />;
}

