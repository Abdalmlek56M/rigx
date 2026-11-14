import type { ReactNode } from "react";
import { CartDrawer } from "@/features/cart/CartDrawer";
import { CartProvider } from "@/features/cart/cart-context";
import { CartToast } from "@/features/cart/CartToast";
import { loadStorefront } from "@/lib/storefront";
import { BackToTop } from "./BackToTop";
import { FloatingContact } from "./FloatingContact";
import { Footer } from "./Footer";
import { Header } from "./Header";
import { TopBar } from "./TopBar";

/** The common frame of every page: top bar, header, content, footer, cart drawer and floating controls. */
export async function SiteShell({ children }: { children: ReactNode }) {
  const storefront = await loadStorefront();
  const floatingLinks = storefront.contactLinks.filter((link) => link.showInFloatingButton);

  return (
    <CartProvider>
      <TopBar storefront={storefront} />
      <Header storefront={storefront} />
      <main className="min-h-[60vh]">{children}</main>
      <Footer storefront={storefront} />
      <CartDrawer />
      <CartToast />
      <FloatingContact links={floatingLinks} />
      <BackToTop />
    </CartProvider>
  );
}
