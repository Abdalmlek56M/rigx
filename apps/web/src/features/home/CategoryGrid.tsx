import { useTranslations } from "next-intl";
import { categoryHref } from "@/components/layout/nav-links";
import { CategoryIcon } from "@/components/ui/category-icon";
import { Link } from "@/i18n/navigation";
import type { MenuCategory } from "@/lib/storefront";
import { CategoryStrip } from "./CategoryStrip";

/** "الأقسام": the top-level menu categories as cards in a moving strip with arrows. */
export function CategoryGrid({ categories }: { categories: MenuCategory[] }) {
  const t = useTranslations("home.categories");
  if (categories.length === 0) return null;

  return (
    <CategoryStrip title={t("title")}>
      {categories.map((category) => (
        <li key={category.slug} className="flex w-[152px] shrink-0 snap-start md:w-[176px]">
          <Link
            href={categoryHref(category.slug)}
            className="lift press flex min-h-[124px] w-full flex-col items-start justify-between gap-4 rounded-card border border-line bg-surface p-4 text-base font-bold transition-colors hover:border-orange/50"
          >
            <span className="flex h-11 w-11 items-center justify-center rounded-full bg-orange-soft text-orange-text"><CategoryIcon slug={category.slug} size={22} /></span>
            <span className="line-clamp-2">{category.name}</span>
          </Link>
        </li>
      ))}
    </CategoryStrip>
  );
}
