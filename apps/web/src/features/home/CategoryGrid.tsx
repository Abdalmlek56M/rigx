import { useLocale, useTranslations } from "next-intl";
import { categoryHref } from "@/components/layout/nav-links";
import { cardClass } from "@/components/ui/Card";
import { CategoryIcon } from "@/components/ui/category-icon";
import { Link } from "@/i18n/navigation";
import { localizedName } from "@/lib/localized-name";
import { mediaUrl } from "@/lib/media";
import type { MenuCategory } from "@/lib/storefront";
import { Rail } from "./Rail";

/**
 * "الأقسام": the top-level menu categories as a draggable, auto-advancing rail. Every card has the same size: the category
 * picture the owner uploaded (usually a transparent PNG) or, without one, the category icon, centred, with the name under it.
 */
export function CategoryGrid({ categories }: { categories: MenuCategory[] }) {
  const t = useTranslations("home.categories");
  const locale = useLocale();
  if (categories.length === 0) return null;

  return (
    <Rail title={t("title")} autoAdvance>
      {categories.map((category) => (
        <li key={category.slug} className="flex w-[168px] shrink-0 md:w-[208px]">
          <Link
            href={categoryHref(category.slug)}
            draggable={false}
            className={`${cardClass({ padding: "sm", interactive: true })} group min-h-[152px] items-center justify-center gap-3 text-center text-base font-bold hover:border-orange/50 md:min-h-[168px]`}
          >
            <span className="flex h-[84px] w-full items-center justify-center md:h-24">
              {category.image ? (
                // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
                <img
                  src={mediaUrl(category.image)}
                  alt=""
                  width={160}
                  height={96}
                  loading="lazy"
                  draggable={false}
                  className="pointer-events-none max-h-full max-w-full object-contain transition-transform duration-300 ease-out group-hover:scale-105"
                />
              ) : (
                <span className="flex size-16 items-center justify-center rounded-full bg-orange-soft text-orange-text transition-transform duration-300 ease-out group-hover:scale-105 md:size-[72px]"><CategoryIcon slug={category.slug} size={30} /></span>
              )}
            </span>
            <span className="line-clamp-2">{localizedName(category, locale)}</span>
          </Link>
        </li>
      ))}
    </Rail>
  );
}
