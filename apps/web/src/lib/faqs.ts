import "server-only";
import { unwrap, type Schemas } from "@rigx/api-client";
import { getLocale } from "next-intl/server";
import type { FaqEntry } from "@/components/ui/FaqList";
import { required } from "./api/required";
import { createServerApi } from "./api/server";

async function toEntries(faqs: Schemas["FaqView"][]): Promise<FaqEntry[]> {
  const english = (await getLocale()) === "en";
  return faqs.map((faq) => ({
    id: faq.id,
    question: english && faq.questionEn ? faq.questionEn : faq.questionAr,
    answer: english && faq.answerEn ? faq.answerEn : faq.answerAr
  }));
}

/** The store's general FAQs (without the help-page-only ones), at most `limit`. Never throws: an API outage means no FAQs. */
export async function loadGeneralFaqs(limit: number): Promise<FaqEntry[]> {
  try {
    const faqs = await required(unwrap(createServerApi().GET("/api/v1/content/faqs")));
    return await toEntries(faqs.filter((faq) => !faq.helpPageOnly).slice(0, limit));
  } catch {
    return [];
  }
}

/** FAQs for one item (item-specific and the general ones that apply to it). Never throws. */
export async function loadItemFaqs(code: string): Promise<FaqEntry[]> {
  try {
    const faqs = await required(unwrap(createServerApi().GET("/api/v1/content/faqs/item/{code}", { params: { path: { code } } })));
    return await toEntries(faqs);
  } catch {
    return [];
  }
}
