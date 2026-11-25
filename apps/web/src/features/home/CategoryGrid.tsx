import { useLocale, useTranslations } from "next-intl";
import { categoryHref } from "@/components/layout/nav-links";
import { cardClass } from "@/components/ui/Card";
import { CategoryIcon } from "@/components/ui/category-icon";
import { ChevronStartIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { localizedName } from "@/lib/localized-name";
import { mediaUrl } from "@/lib/media";
import type { MenuCategory } from "@/lib/storefront";
import { Rail } from "./Rail";

/**
 * "ما القطعة التي تبحث عنها؟": the top-level menu categories as a draggable, auto-advancing rail of large cards. Each card has a
 * stage with a soft orange glow holding the category picture the owner uploaded (usually a transparent PNG) or, without one,
 * its icon in a large circle; under it the name and an arrow that slides on hover.
 */
export function CategoryGrid({ categories }: { categories: MenuCategory[] }) {
  const t = useTranslations("home.categories");
  const locale = useLocale();
  if (categories.length === 0) return null;

  return (
    <Rail title={t("title")} autoAdvance>
      {categories.map((category) => (
        <li key={category.slug} className="flex w-[158px] shrink-0 md:w-[204px]">
          <Link
            href={categoryHref(category.slug)}
            draggable={false}
            className={`${cardClass({ padding: "none", interactive: true })} path-tile group w-full overflow-hidden hover:border-orange/50`}
          >
            <span className="category-stage flex h-32 w-full items-center justify-center p-4 md:h-[164px] md:p-5">
              {category.image ? (
                // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
                <img
                  src={mediaUrl(category.image)}
                  alt=""
                  width={200}
                  height={160}
                  loading="lazy"
                  draggable={false}
                  className="pointer-events-none max-h-full max-w-full rounded-control object-contain drop-shadow-[0_10px_14px_rgb(0_0_0/0.14)] transition-transform duration-300 ease-out group-hover:scale-105"
                />
              ) : (
                <span className="flex size-[72px] items-center justify-center rounded-full bg-surface text-orange-text shadow-soft transition-transform duration-300 ease-out group-hover:scale-105 md:size-20">
                  <CategoryIcon slug={category.slug} icon={category.icon} size={38} />
                </span>
              )}
            </span>
            <span className="flex items-center justify-between gap-2 border-t border-line px-3.5 py-3 md:px-4">
              <span className="line-clamp-1 text-[15px] font-bold md:text-base">{localizedName(category, locale)}</span>
              <span aria-hidden="true" className="path-arrow flex text-orange-text ltr:-scale-x-100"><ChevronStartIcon size={18} /></span>
            </span>
          </Link>
        </li>
      ))}
    </Rail>
  );
}
