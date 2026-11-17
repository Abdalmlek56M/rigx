import "server-only";
import { unwrap } from "@rigx/api-client";
import { required } from "@/lib/api/required";
import { createServerApi } from "@/lib/api/server";
import { groupFaqs, type FaqGroup } from "./faq-groups";

/** The help page's FAQ groups. Never throws: an API outage means no questions. */
export async function loadFaqPage(english: boolean): Promise<FaqGroup[]> {
  try {
    return groupFaqs(await required(unwrap(createServerApi().GET("/api/v1/content/faqs"))), english);
  } catch {
    return [];
  }
}
