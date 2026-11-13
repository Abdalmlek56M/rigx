"use client";

import type { ReactNode } from "react";
import { LocaleProvider } from "@/lib/i18n";
import { SessionProvider } from "@/components/session-provider";
import { WishlistProvider } from "@/components/wishlist-provider";
import { ThemeProvider } from "@/components/theme-provider";
import { CartProvider } from "@/components/cart-provider";
import { HomeLayoutProvider } from "@/components/home-layout-provider";
import { StoreSettingsProvider } from "@/components/store-settings-provider";

export function AppProviders({ children, initialLocale = "en" }: { children: ReactNode; initialLocale?: "ar" | "en" }) {
  return (
    <ThemeProvider>
      <LocaleProvider initialLocale={initialLocale}>
        <StoreSettingsProvider>
          <CartProvider>
            <HomeLayoutProvider>
              <SessionProvider>
                <WishlistProvider>{children}</WishlistProvider>
              </SessionProvider>
            </HomeLayoutProvider>
          </CartProvider>
        </StoreSettingsProvider>
      </LocaleProvider>
    </ThemeProvider>
  );
}
