import { useTranslations } from "next-intl";
import { categoryHref } from "@/components/layout/nav-links";
import { CategoryIcon } from "@/components/ui/category-icon";
import { Link } from "@/i18n/navigation";
import type { MenuCategory } from "@/lib/storefront";

/** "تصفح حسب القطعة": the top-level menu categories as cards. */
export function CategoryGrid({ categories }: { categories: MenuCategory[] }) {
  const t = useTranslations("home.categories");
  if (categories.length === 0) return null;

  return (
    <section className="flex shell flex-col gap-5 pb-6 pt-10 md:pt-14">
      <h2 className="m-0 text-[22px] font-bold md:text-[30px]">{t("title")}</h2>
      <ul className="m-0 grid list-none grid-cols-[repeat(auto-fill,minmax(150px,1fr))] gap-3 p-0 xl:grid-cols-[repeat(auto-fill,minmax(170px,1fr))]">
        {categories.map((category) => (
          <li key={category.slug} className="flex [&>a]:w-full">
            <Link
              href={categoryHref(category.slug)}
              className="lift press flex h-full min-h-[120px] flex-col items-start justify-between gap-4 rounded-card border border-line bg-surface p-4 text-base font-bold"
            >
              <span className="flex h-11 w-11 items-center justify-center rounded-full bg-orange-soft text-orange-text"><CategoryIcon slug={category.slug} size={22} /></span>
              {category.name}
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
