"use client";

import Script from "next/script";
import { useLocale } from "next-intl";
import { useCallback, useEffect, useRef, useState } from "react";

const CLIENT_ID = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;

/** Google sign-in is offered only when a client id is configured. */
export const GOOGLE_SIGN_IN_ENABLED = Boolean(CLIENT_ID);

interface GoogleButtonProps {
  /** Called with the Google ID token once the person picks an account. */
  onCredential: (idToken: string) => void;
}

/** Google's own "Continue with Google" button as a full-width pill (black in dark mode); renders nothing when no client id is configured. */
export function GoogleButton({ onCredential }: GoogleButtonProps) {
  const locale = useLocale();
  const host = useRef<HTMLDivElement>(null);
  const callback = useRef(onCredential);
  const [ready, setReady] = useState(() => typeof window !== "undefined" && window.google !== undefined);

  useEffect(() => {
    callback.current = onCredential;
  }, [onCredential]);

  const render = useCallback(() => {
    const element = host.current;
    const id = window.google?.accounts.id;
    if (!element || !id || !CLIENT_ID) return;
    id.initialize({ client_id: CLIENT_ID, callback: (response) => callback.current(response.credential) });
    id.renderButton(element, {
      theme: document.documentElement.dataset.theme === "dark" ? "filled_black" : "outline",
      size: "large",
      shape: "pill",
      text: "continue_with",
      logo_alignment: "center",
      locale,
      width: Math.min(400, Math.max(200, element.clientWidth))
    });
  }, [locale]);

  useEffect(() => {
    if (ready) render();
  }, [ready, render]);

  if (!CLIENT_ID) return null;
  return (
    <>
      <Script src="https://accounts.google.com/gsi/client" strategy="afterInteractive" onReady={() => setReady(true)} />
      {/* color-scheme light matches Google's frame, so the browser paints no white box behind the pill in dark mode. */}
      <div ref={host} className="flex min-h-11 justify-center [color-scheme:light]" />
    </>
  );
}
