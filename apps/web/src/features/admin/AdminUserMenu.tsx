"use client";

import { useCallback, useEffect, useId, useRef, useState, type FocusEvent, type KeyboardEvent } from "react";
import { useLocale, useTranslations } from "next-intl";
import { setTheme, useTheme, type Theme } from "@/components/layout/use-theme";
import { ChevronDownIcon, LogoutIcon, MoonIcon, SunIcon, UserIcon } from "@/components/ui/icons";
import type { SessionUser } from "@/features/auth/session-store";
import { Link, usePathname } from "@/i18n/navigation";

const KNOWN_ROLES = ["Owner"] as const;
type KnownRole = (typeof KNOWN_ROLES)[number];
const isKnownRole = (code: string): code is KnownRole => KNOWN_ROLES.some((known) => known === code);

const ITEM =
  "flex min-h-11 w-full items-center gap-2.5 rounded-control px-2.5 text-start text-sm " +
  "hover:bg-paper focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange";
const CHOICE =
  "flex min-h-11 items-center justify-center gap-1.5 rounded-control border border-line px-2 text-[13px] font-bold " +
  "text-ink-muted hover:text-ink focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange " +
  "aria-checked:border-orange-text aria-checked:bg-paper aria-checked:text-ink";

interface AdminUserMenuProps {
  user: SessionUser;
  onSignOut: () => void;
  /** Closes the mobile drawer after a link (the menu itself always closes). */
  onNavigate?: () => void;
}

/** The sidebar footer: one button with the avatar, name, role and a menu above it (theme, language, profile, sign out). */
export function AdminUserMenu({ user, onSignOut, onNavigate }: AdminUserMenuProps) {
  const t = useTranslations("admin.shell");
  const locale = useLocale();
  const pathname = usePathname();
  const theme = useTheme();
  const [open, setOpen] = useState(false);
  const [shown, setShown] = useState(false);
  const root = useRef<HTMLDivElement>(null);
  const trigger = useRef<HTMLButtonElement>(null);
  const menu = useRef<HTMLDivElement>(null);
  const menuId = useId();

  const name = user.fullName || user.email;
  const role = user.roles.find(isKnownRole);
  const roleLabel = role ? t(`roles.${role}`) : t("roles.staff");

  const close = useCallback((focusTrigger: boolean) => {
    setOpen(false);
    setShown(false);
    if (focusTrigger) trigger.current?.focus();
  }, []);

  useEffect(() => {
    if (!open) return;
    const frame = window.requestAnimationFrame(() => setShown(true));
    const outside = (event: PointerEvent) => {
      if (root.current && event.target instanceof Node && !root.current.contains(event.target)) {
        setOpen(false);
        setShown(false);
      }
    };
    document.addEventListener("pointerdown", outside);
    menu.current?.querySelector<HTMLElement>('[role^="menuitem"]')?.focus();
    return () => {
      window.cancelAnimationFrame(frame);
      document.removeEventListener("pointerdown", outside);
    };
  }, [open]);

  function toggle() {
    if (open) {
      setOpen(false);
      setShown(false);
    } else {
      setShown(false);
      setOpen(true);
    }
  }

  function chooseTheme(next: Theme) {
    setTheme(next);
    close(true);
  }

  function menuKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (!open) return;
    if (event.key === "Escape") {
      event.preventDefault();
      close(true);
      return;
    }
    const items = Array.from(menu.current?.querySelectorAll<HTMLElement>('[role^="menuitem"]') ?? []);
    if (items.length === 0) return;
    const current = items.indexOf(document.activeElement as HTMLElement);
    if (event.key === "ArrowDown") {
      event.preventDefault();
      items[current < 0 ? 0 : (current + 1) % items.length]?.focus();
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      items[current < 0 ? items.length - 1 : (current - 1 + items.length) % items.length]?.focus();
    } else if (event.key === "Home") {
      event.preventDefault();
      items[0]?.focus();
    } else if (event.key === "End") {
      event.preventDefault();
      items[items.length - 1]?.focus();
    }
  }

  function menuBlur(event: FocusEvent<HTMLDivElement>) {
    if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
      setOpen(false);
      setShown(false);
    }
  }

  const languages = [
    { locale: "ar" as const, label: t("menu.arabic"), badge: "ع" },
    { locale: "en" as const, label: t("menu.english"), badge: "EN" }
  ];

  return (
    <div ref={root} className="relative" onKeyDown={menuKeyDown} onBlur={menuBlur}>
      <button
        ref={trigger} type="button" aria-haspopup="menu" aria-expanded={open} aria-controls={open ? menuId : undefined}
        onClick={toggle}
        onKeyDown={(event) => {
          if (event.key === "ArrowDown" || event.key === "ArrowUp") {
            event.preventDefault();
            setOpen(true);
          }
        }}
        className="flex min-h-11 w-full items-center gap-2.5 rounded-control p-2 text-start hover:bg-footer-line focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange"
      >
        <span aria-hidden="true" className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-footer-line font-bold text-on-carbon">
          {name.charAt(0).toUpperCase()}
        </span>
        <span className="flex min-w-0 flex-1 flex-col text-[13px]">
          <b className="truncate text-on-carbon">{name}</b>
          <span className="truncate text-xs text-footer-text">{roleLabel}</span>
        </span>
        <span aria-hidden="true" className={`text-footer-text transition-transform duration-150 motion-reduce:transition-none ${open ? "rotate-180" : ""}`}>
          <ChevronDownIcon size={16} />
        </span>
      </button>

      {open ? (
        <div
          ref={menu} id={menuId} role="menu" aria-label={t("menu.label")}
          className={`absolute bottom-full end-0 z-30 mb-2 flex w-52 flex-col gap-1 rounded-card border border-line bg-surface p-1.5 text-ink shadow-lift transition-[opacity,transform] duration-150 ease-out motion-reduce:transform-none motion-reduce:transition-none ${shown ? "translate-y-0 opacity-100" : "translate-y-1 opacity-0"}`}
        >
          <div role="group" aria-label={t("menu.appearance")} className="flex flex-col gap-1 px-1 pt-0.5">
            <span aria-hidden="true" className="px-1 text-[11px] font-bold text-ink-muted">{t("menu.appearance")}</span>
            <div className="grid grid-cols-2 gap-1">
              <button type="button" role="menuitemradio" aria-checked={theme === "light"} onClick={() => chooseTheme("light")} className={CHOICE}>
                <SunIcon size={16} /> {t("menu.light")}
              </button>
              <button type="button" role="menuitemradio" aria-checked={theme === "dark"} onClick={() => chooseTheme("dark")} className={CHOICE}>
                <MoonIcon size={16} /> {t("menu.dark")}
              </button>
            </div>
          </div>

          <div role="group" aria-label={t("menu.language")} className="flex flex-col gap-1 px-1">
            <span aria-hidden="true" className="px-1 text-[11px] font-bold text-ink-muted">{t("menu.language")}</span>
            <div className="grid grid-cols-2 gap-1">
              {languages.map((language) => (
                <Link
                  key={language.locale} href={pathname} locale={language.locale} hrefLang={language.locale}
                  role="menuitemradio" aria-checked={locale === language.locale}
                  onClick={() => {
                    close(false);
                    onNavigate?.();
                  }}
                  className={CHOICE}
                >
                  <span aria-hidden="true" className="flex h-4 min-w-5 items-center justify-center rounded-[3px] border border-line px-0.5 font-mono text-[10px]">
                    {language.badge}
                  </span>
                  {language.label}
                </Link>
              ))}
            </div>
          </div>

          <div role="separator" className="my-0.5 h-px bg-line" />

          <Link
            href="/account/profile" role="menuitem"
            onClick={() => {
              close(false);
              onNavigate?.();
            }}
            className={`${ITEM} text-ink`}
          >
            <UserIcon size={18} /> {t("menu.profile")}
          </Link>

          <div role="separator" className="my-0.5 h-px bg-line" />

          <button
            type="button" role="menuitem"
            onClick={() => {
              close(false);
              onSignOut();
            }}
            className={`${ITEM} text-error hover:bg-error/10`}
          >
            <LogoutIcon size={18} /> {t("menu.signOut")}
          </button>
        </div>
      ) : null}
    </div>
  );
}
