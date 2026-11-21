"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ModalSheet } from "@/components/ui/ModalSheet";
import { IconButton } from "@/components/ui/Button";
import { CloseIcon, MenuIcon, UserIcon } from "@/components/ui/icons";
import { AccountLink } from "@/features/auth/AccountLink";
import { Link } from "@/i18n/navigation";
import { localizedName } from "@/lib/localized-name";
import type { MenuCategory } from "@/lib/storefront";
import { LocaleSwitch } from "./LocaleSwitch";
import { NAV_LINKS, categoryHref } from "./nav-links";
import { ThemeToggle } from "./ThemeToggle";

const itemClass = "flex min-h-11 items-center rounded-card px-3 text-sm hover:bg-paper";

/** Navigation sheet for phones and for md–xl, where the full header links do not fit: categories, links, theme, language. */
export function MobileMenu({ categories, englishEnabled }: { categories: MenuCategory[]; englishEnabled: boolean }) {
  const t = useTranslations("header");
  const tc = useTranslations("common");
  const locale = useLocale();
  const [open, setOpen] = useState(false);
  const close = () => setOpen(false);

  return (
    <div className="xl:hidden">
      <IconButton
        icon={MenuIcon}
        label={t("menu")}
        variant="ghost"
        size="md"
        aria-haspopup="dialog"
        aria-expanded={open}
        onClick={() => setOpen(true)}
      />
      <ModalSheet open={open} onClose={close} label={t("menu")} edge="start" fullHeight>
        <div className="flex h-full flex-col">
          <div className="flex items-center justify-between border-b border-line px-4 py-3">
            <b className="type-section-title">{t("menu")}</b>
            <IconButton icon={CloseIcon} label={tc("close")} variant="secondary" onClick={close} />
          </div>
          <nav aria-label={t("navLabel")} onClick={(event) => (event.target as HTMLElement).closest("a") && close()} className="min-h-0 flex-1 overflow-y-auto overscroll-contain p-3">
            {categories.length > 0 ? (
              <ul className="mb-3 border-b border-line pb-3">
                {categories.map((category) => (
                  <li key={category.slug}>
                    <Link href={categoryHref(category.slug)} className={`${itemClass} font-bold`}>
                      {localizedName(category, locale)}
                    </Link>
                    <ul className="ps-3">
                      {category.children.map((child) => (
                        <li key={child.slug}>
                          <Link href={categoryHref(child.slug)} className={itemClass}>
                            {localizedName(child, locale)}
                          </Link>
                        </li>
                      ))}
                    </ul>
                  </li>
                ))}
              </ul>
            ) : null}
            <ul className="m-0 flex list-none flex-col p-0">
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
          <div className="flex items-center gap-2 border-t border-line p-3">
            <ThemeToggle />
            {englishEnabled ? <LocaleSwitch /> : null}
          </div>
        </div>
      </ModalSheet>
    </div>
  );
}
