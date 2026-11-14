"use client";

import { useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { ChevronDownIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { useDismiss } from "@/lib/hooks/use-dismiss";
import type { MenuCategory } from "@/lib/storefront";
import { categoryHref } from "./nav-links";

/** "All categories" disclosure: top-level categories with their children nested one level. */
export function CategoriesMenu({ categories }: { categories: MenuCategory[] }) {
  const t = useTranslations("header");
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const close = () => setOpen(false);
  useDismiss(ref, open, close);

  if (categories.length === 0) return null;

  return (
    <div ref={ref} className="relative">
      <button
        type="button"
        aria-expanded={open}
        aria-controls="categories-panel"
        onClick={() => setOpen((value) => !value)}
        className="flex min-h-11 items-center gap-1 text-[15px]"
      >
        {t("allCategories")}
        <ChevronDownIcon size={16} />
      </button>
      {open ? (
        <ul
          id="categories-panel"
          className="absolute start-0 top-full z-30 mt-1 max-h-[70vh] w-72 overflow-y-auto rounded-panel border border-line bg-surface p-2 text-ink shadow-[0_10px_30px_rgba(0,0,0,.15)]"
        >
          {categories.map((category) => (
            <li key={category.slug}>
              <Link href={categoryHref(category.slug)} onClick={close} className="block rounded-card px-3 py-2.5 font-bold hover:bg-paper">
                {category.name}
              </Link>
              {category.children.length > 0 ? (
                <ul className="ps-3">
                  {category.children.map((child) => (
                    <li key={child.slug}>
                      <Link href={categoryHref(child.slug)} onClick={close} className="block rounded-card px-3 py-2 text-sm hover:bg-paper">
                        {child.name}
                      </Link>
                    </li>
                  ))}
                </ul>
              ) : null}
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
