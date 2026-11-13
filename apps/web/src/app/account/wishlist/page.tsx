import type { Metadata } from "next";
import { AccountWishlist } from "@/components/account-wishlist";

export const metadata: Metadata = {
  title: "Wishlist | RIGX",
  robots: { index: false, follow: false },
};

export default function AccountWishlistPage() {
  return <AccountWishlist />;
}
