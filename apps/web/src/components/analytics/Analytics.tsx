"use client";

import Script from "next/script";
import { usePathname } from "next/navigation";
import { useEffect, useRef } from "react";
import { cleanId, GA_ID_PATTERN, isMeasuredPath, isWhatsAppLink, META_PIXEL_ID_PATTERN } from "./analytics-rules";

const GA_ID = cleanId(process.env.NEXT_PUBLIC_GA_ID, GA_ID_PATTERN);
const PIXEL_ID = cleanId(process.env.NEXT_PUBLIC_META_PIXEL_ID, META_PIXEL_ID_PATTERN);

/**
 * Google Analytics 4 and the Meta pixel, loaded only when their ids are set and never on admin pages.
 * GA4 counts client-side page changes itself (enhanced measurement); the pixel gets a PageView per route.
 * A click on any WhatsApp link is a lead: `generate_lead` in GA4, `Contact` in Meta.
 */
export function Analytics() {
  const pathname = usePathname();
  const measured = isMeasuredPath(pathname);
  const firstPage = useRef(true);

  useEffect(() => {
    if (!measured) return;
    if (firstPage.current) {
      firstPage.current = false; // the pixel snippet already sent the first PageView
      return;
    }
    window.fbq?.("track", "PageView");
  }, [pathname, measured]);

  useEffect(() => {
    if (!measured || (!GA_ID && !PIXEL_ID)) return;
    const onClick = (event: MouseEvent) => {
      const link = (event.target as Element | null)?.closest?.("a");
      if (!isWhatsAppLink(link?.href)) return;
      window.gtag?.("event", "generate_lead", { method: "whatsapp" });
      window.fbq?.("track", "Contact");
    };
    document.addEventListener("click", onClick, { capture: true });
    return () => document.removeEventListener("click", onClick, { capture: true });
  }, [measured]);

  if (!measured) return null;
  return (
    <>
      {GA_ID && (
        <>
          <Script src={`https://www.googletagmanager.com/gtag/js?id=${GA_ID}`} strategy="afterInteractive" />
          <Script id="rigx-ga" strategy="afterInteractive">
            {`window.dataLayer=window.dataLayer||[];function gtag(){dataLayer.push(arguments);}gtag('js',new Date());gtag('config','${GA_ID}');`}
          </Script>
        </>
      )}
      {PIXEL_ID && (
        <Script id="rigx-pixel" strategy="afterInteractive">
          {`!function(f,b,e,v,n,t,s){if(f.fbq)return;n=f.fbq=function(){n.callMethod?n.callMethod.apply(n,arguments):n.queue.push(arguments)};if(!f._fbq)f._fbq=n;n.push=n;n.loaded=!0;n.version='2.0';n.queue=[];t=b.createElement(e);t.async=!0;t.src=v;s=b.getElementsByTagName(e)[0];s.parentNode.insertBefore(t,s)}(window,document,'script','https://connect.facebook.net/en_US/fbevents.js');fbq('init','${PIXEL_ID}');fbq('track','PageView');`}
        </Script>
      )}
    </>
  );
}
