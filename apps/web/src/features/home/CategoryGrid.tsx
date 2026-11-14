import { useTranslations } from "next-intl";
import { categoryHref } from "@/components/layout/nav-links";
import { Link } from "@/i18n/navigation";
import type { MenuCategory } from "@/lib/storefront";

/** "تصفح حسب القطعة": the top-level menu categories as cards. */
export function CategoryGrid({ categories }: { categories: MenuCategory[] }) {
  const t = useTranslations("home.categories");
  if (categories.length === 0) return null;

  return (
    <section className="mx-auto flex max-w-[1240px] flex-col gap-5 px-4 pb-6 pt-10 md:px-6 md:pt-14">
      <h2 className="m-0 text-[22px] font-bold md:text-[30px]">{t("title")}</h2>
      <ul className="m-0 grid list-none grid-cols-[repeat(auto-fill,minmax(140px,1fr))] gap-3 p-0">
        {categories.map((category) => (
          <li key={category.slug}>
            <Link
              href={categoryHref(category.slug)}
              className="flex min-h-14 items-center rounded-card border border-line bg-surface px-3.5 py-[18px] text-base font-bold"
            >
              {category.name}
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
