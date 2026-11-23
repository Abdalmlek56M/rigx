"use client";

import { useLocale } from "next-intl";
import { TextField, type TextFieldProps } from "@/components/ui/TextField";

/**
 * Every date the admin asks for (an expense, a purchase, a period, a coupon's run, a partner's join date): the page's language is set
 * on the field and its text is laid out left to right and aligned to the start, like every other number field. The browser still draws
 * the parts in its own order (`mm/dd/yyyy` on an English browser); a page cannot change that without a custom picker.
 */
export function DateField(props: Omit<TextFieldProps, "type" | "ltr" | "lang">) {
  const locale = useLocale();
  return <TextField {...props} type="date" ltr lang={locale} />;
}
