"use client";

import { useLocale } from "@/lib/i18n";
import { Message, SecondaryButton } from "@/components/ui";

export default function AdminError({ reset }: { reset: () => void }) {
  const { messages } = useLocale();
  return (
    <main className="p-5 sm:p-8">
      <Message tone="error">{messages.adminSessionUnavailable}</Message>
      <SecondaryButton type="button" onClick={reset} className="mt-4">
        {messages.retry}
      </SecondaryButton>
    </main>
  );
}
