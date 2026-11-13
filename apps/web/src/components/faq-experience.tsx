"use client";

import { useEffect, useState } from "react";
import { getPublicFaqs, type ContentFaq } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { LoadingCard, Message, SecondaryButton } from "@/components/ui";

export function FaqExperience() {
  const { messages, locale } = useLocale();
  const [faqs, setFaqs] = useState<ContentFaq[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    void getPublicFaqs()
      .then((items) => {
        if (active) setFaqs(items);
      })
      .catch((requestError) => {
        if (active) setError(apiErrorMessage(requestError, messages));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [attempt, messages]);

  const categories = Array.from(new Set(faqs.map((faq) => faq.category).filter((category): category is string => Boolean(category))));

  return (
    <>
      <div className="page-hero compact-hero">
      <div>
        <span className="eyebrow">RIGX / HELP CENTER</span>
        <h1>{messages.faqTitle}</h1>
        <p>{messages.faqLead}</p>
      </div></div>

      {loading ? <LoadingCard /> : error ? (
        <div className="grid gap-3">
          <Message tone="error">{error}</Message>
          <SecondaryButton type="button" onClick={() => setAttempt((value) => value + 1)}>{messages.retry}</SecondaryButton>
        </div>
      ) : faqs.length === 0 ? (
        <Message tone="info">{messages.faqEmpty}</Message>
      ) : (
        <div className="home-faq-grid">
          {categories.length > 0 ? (
            <ul className="flex flex-wrap gap-2">
              {categories.map((category) => (
                <li key={category} className="rounded-full border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] px-3 py-1 text-xs font-semibold text-[var(--ds-text-muted)]">
                  {category}
                </li>
              ))}
            </ul>
          ) : null}
          {faqs.map((faq) => (
            <details key={faq.id} className="home-faq-item glass-panel">
              <summary>
                {locale === "ar" ? faq.questionAr : faq.questionEn}
              </summary>
              <p>
                {locale === "ar" ? faq.answerAr : faq.answerEn}
              </p>
            </details>
          ))}
        </div>
      )}
    </>
  );
}

