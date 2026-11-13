"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { confirmEmailChange } from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, PageIntro } from "@/components/ui";

export function EmailChangeConfirm() {
  const { messages } = useLocale();
  const [state, setState] = useState<"loading" | "success" | "error">("loading");

  useEffect(() => {
    const hash = window.location.hash;
    const token = new URLSearchParams(hash.startsWith("#") ? hash.slice(1) : hash).get("token");
    window.history.replaceState(null, "", window.location.pathname);
    if (!token) {
      setState("error");
      return;
    }
    void confirmEmailChange(token)
      .then(() => setState("success"))
      .catch(() => setState("error"));
  }, []);

  if (state === "loading") return <LoadingCard />;
  return (
    <main className="mx-auto w-full max-w-xl px-5 py-12">
      <PageIntro
        eyebrow="RIGX / ACCOUNT"
        title={messages.emailChangeConfirmTitle}
        lead={state === "success" ? messages.emailChangeConfirmed : messages.emailChangeInvalid}
      />
      {state === "success" ? (
        <div className="mt-4 grid gap-3">
          <Message tone="success">{messages.emailChangeConfirmed}</Message>
          <Link href="/login" className="rigx-button rigx-button-primary w-fit">{messages.navLogin}</Link>
        </div>
      ) : (
        <div className="mt-4 grid gap-3">
          <Message tone="error">{messages.emailChangeInvalid}</Message>
          <Link href="/account/profile" className="rigx-button rigx-button-secondary w-fit">{messages.accountBackToOverview}</Link>
        </div>
      )}
    </main>
  );
}
