import type { Locale } from "@/lib/i18n";

const arabicCategoryNames: Record<string, string> = {
  processors: "المعالجات",
  "amd-processors": "معالجات AMD",
  "intel-processors": "معالجات Intel",
  "graphics-cards": "كروت الشاشة",
  motherboards: "اللوحات الأم",
  memory: "الذاكرة",
  storage: "التخزين",
  cooling: "التبريد",
  "power-supplies": "مزودات الطاقة",
  cases: "صناديق الحاسوب",
  peripherals: "الملحقات",
};

const englishCategoryNames: Record<string, string> = {
  processors: "Processors",
  "amd-processors": "AMD Processors",
  "intel-processors": "Intel Processors",
  "graphics-cards": "Graphics Cards",
  motherboards: "Motherboards",
  memory: "Memory",
  storage: "Storage",
  cooling: "Cooling",
  "power-supplies": "Power Supplies",
  cases: "Cases",
  peripherals: "Peripherals",
};

export function localizedCategoryName(slug: string, fallback: string, locale: Locale): string {
  return locale === "ar" ? arabicCategoryNames[slug] ?? fallback : englishCategoryNames[slug] ?? fallback;
}

export function technicalCategoryName(slug: string, fallback: string): string {
  return englishCategoryNames[slug] ?? fallback;
}
