"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { ModalSheet } from "@/components/ui/ModalSheet";
import { CloseIcon, MenuIcon, UserIcon } from "@/components/ui/icons";
import { AccountLink } from "@/features/auth/AccountLink";
import { Link } from "@/i18n/navigation";
import type { MenuCategory } from "@/lib/storefront";
import { LocaleSwitch } from "./LocaleSwitch";
import { NAV_LINKS, categoryHref } from "./nav-links";
import { ThemeToggle } from "./ThemeToggle";

const itemClass = "flex min-h-11 items-center rounded-card px-3 hover:bg-paper";

/** Mobile navigation sheet: categories, the header links, account, theme and language. */
export function MobileMenu({ categories, englishEnabled }: { categories: MenuCategory[]; englishEnabled: boolean }) {
  const t = useTranslations("header");
  const tc = useTranslations("common");
  const [open, setOpen] = useState(false);
  const close = () => setOpen(false);

  return (
    <div className="md:hidden">
      <button
        type="button"
        aria-label={t("menu")}
        aria-haspopup="dialog"
        onClick={() => setOpen(true)}
        className="flex h-11 w-11 items-center justify-center text-on-carbon"
      >
        <MenuIcon size={22} />
      </button>
      <ModalSheet open={open} onClose={close} label={t("menu")} edge="start" fullHeight>
        <div className="flex h-full flex-col">
          <div className="flex items-center justify-between border-b border-line px-4 py-3">
            <b>{t("menu")}</b>
            <button
              type="button"
              aria-label={tc("close")}
              onClick={close}
              className="flex h-11 w-11 items-center justify-center rounded-control border border-input-line"
            >
              <CloseIcon size={18} />
            </button>
          </div>
          <nav aria-label={t("navLabel")} onClick={(event) => (event.target as HTMLElement).closest("a") && close()} className="min-h-0 flex-1 overflow-y-auto p-3">
            {categories.length > 0 ? (
              <ul className="mb-3 border-b border-line pb-3">
                {categories.map((category) => (
                  <li key={category.slug}>
                    <Link href={categoryHref(category.slug)} className={`${itemClass} font-bold`}>
                      {category.name}
                    </Link>
                    <ul className="ps-3">
                      {category.children.map((child) => (
                        <li key={child.slug}>
                          <Link href={categoryHref(child.slug)} className={`${itemClass} text-sm`}>
                            {child.name}
                          </Link>
                        </li>
                      ))}
                    </ul>
                  </li>
                ))}
              </ul>
            ) : null}
            <ul>
              {NAV_LINKS.map((link) => (
                <li key={link.key}>
                  <Link href={link.href} className={`${itemClass} ${link.emphasis ? "font-bold text-orange-text" : ""}`}>
                    {t(link.key)}
                  </Link>
                </li>
              ))}
              <li>
                <AccountLink className={`${itemClass} gap-2`}>
                  <UserIcon size={18} />
                  {t("account")}
                </AccountLink>
              </li>
            </ul>
          </nav>
          <div className="flex gap-2 border-t border-line bg-carbon p-3">
            <ThemeToggle />
            {englishEnabled ? <LocaleSwitch /> : null}
          </div>
        </div>
      </ModalSheet>
    </div>
  );
}
