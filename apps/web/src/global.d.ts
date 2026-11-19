import type { routing } from "@/i18n/routing";
import type ar from "../messages/ar.json";

declare module "next-intl" {
  interface AppConfig {
    Locale: (typeof routing.locales)[number];
    Messages: typeof ar;
  }
}

/** The slice of Google Identity Services (`accounts.google.com/gsi/client`) the storefront uses. */
declare global {
  interface GoogleCredentialResponse {
    credential: string;
  }

  interface GoogleButtonOptions {
    theme: "outline" | "filled_blue" | "filled_black";
    size: "large" | "medium" | "small";
    text: "signin_with" | "signup_with" | "continue_with" | "signin";
    locale: string;
    width: number;
  }

  interface Window {
    /** Google tag (GA4), present only when `NEXT_PUBLIC_GA_ID` is set. */
    gtag?: (...args: unknown[]) => void;
    /** Meta pixel, present only when `NEXT_PUBLIC_META_PIXEL_ID` is set. */
    fbq?: (...args: unknown[]) => void;
    google?: {
      accounts: {
        id: {
          initialize(config: { client_id: string; callback: (response: GoogleCredentialResponse) => void }): void;
          renderButton(parent: HTMLElement, options: GoogleButtonOptions): void;
        };
      };
    };
  }
}
