import { useTranslations } from "next-intl";
import { categoryHref } from "@/components/layout/nav-links";
import { CategoryIcon } from "@/components/ui/category-icon";
import { Link } from "@/i18n/navigation";
import { mediaUrl } from "@/lib/media";
import type { MenuCategory } from "@/lib/storefront";
import { Rail } from "./Rail";

/**
 * "الأقسام": the top-level menu categories as a draggable, auto-advancing rail. Each card has the category icon and, when the
 * owner uploaded one, the category picture (usually a transparent PNG) standing on the card's end side.
 */
export function CategoryGrid({ categories }: { categories: MenuCategory[] }) {
  const t = useTranslations("home.categories");
  if (categories.length === 0) return null;

  return (
    <Rail title={t("title")} autoAdvance>
      {categories.map((category) => (
        <li key={category.slug} className="flex w-[168px] shrink-0 md:w-[208px]">
          <Link
            href={categoryHref(category.slug)}
            draggable={false}
            className="lift press group relative flex min-h-[132px] w-full flex-col items-start justify-between gap-4 overflow-hidden rounded-card border border-line bg-surface p-4 text-base font-bold transition-colors hover:border-orange/50 md:min-h-[148px]"
          >
            <span className="flex h-11 w-11 items-center justify-center rounded-full bg-orange-soft text-orange-text"><CategoryIcon slug={category.slug} size={22} /></span>
            {category.image ? (
              // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
              <img
                src={mediaUrl(category.image)}
                alt=""
                width={112}
                height={112}
                loading="lazy"
                draggable={false}
                className="pointer-events-none absolute end-3 top-3 h-[72px] w-[88px] rounded-[8px] object-contain transition-transform duration-300 ease-out group-hover:-translate-y-0.5 group-hover:scale-[1.04] md:h-[84px] md:w-[108px]"
              />
            ) : null}
            <span className="relative line-clamp-2">{category.name}</span>
          </Link>
        </li>
      ))}
    </Rail>
  );
}
