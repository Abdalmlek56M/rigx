"use client";

import Script from "next/script";
import { useLocale } from "next-intl";
import { useCallback, useEffect, useRef, useState } from "react";

const CLIENT_ID = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;

interface GoogleButtonProps {
  /** Called with the Google ID token once the person picks an account. */
  onCredential: (idToken: string) => void;
}

/** Google's own "Continue with Google" button; renders nothing when no client id is configured. */
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
      theme: "outline",
      size: "large",
      text: "continue_with",
      locale,
      width: Math.min(340, Math.max(200, element.clientWidth))
    });
  }, [locale]);

  useEffect(() => {
    if (ready) render();
  }, [ready, render]);

  if (!CLIENT_ID) return null;
  return (
    <>
      <Script src="https://accounts.google.com/gsi/client" strategy="afterInteractive" onReady={() => setReady(true)} />
      <div ref={host} className="flex min-h-11 justify-center" />
    </>
  );
}
