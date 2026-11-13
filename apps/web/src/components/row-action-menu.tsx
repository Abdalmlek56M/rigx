"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  type KeyboardEvent,
  useCallback,
  useEffect,
  useId,
  useRef,
  useState,
} from "react";
import { createPortal } from "react-dom";
import { Icon, type IconName } from "@/components/icons";

export type RowAction = {
  key: string;
  label: string;
  icon?: IconName;
  href?: string;
  target?: string;
  onSelect?: () => void;
  disabled?: boolean;
  tone?: "default" | "danger";
};

type RowActionMenuProps = {
  label: string;
  actions: RowAction[];
  triggerClassName?: string;
  align?: "start" | "end";
};

let activeMenuCloser: (() => void) | null = null;

/**
 * Shared row-action popover for dense admin tables.
 *
 * Only one menu can be open at a time. It is portaled so table overflow cannot
 * clip it, repositions inside the viewport, and closes on selection, outside
 * click, Escape, route change, page scroll, and resize. Focus is restored to
 * the trigger when the menu closes from the keyboard.
 */
export function RowActionMenu({ label, actions, triggerClassName = "", align = "end" }: RowActionMenuProps) {
  const pathname = usePathname();
  const menuId = useId();
  const triggerRef = useRef<HTMLButtonElement | null>(null);
  const menuRef = useRef<HTMLDivElement | null>(null);
  const [open, setOpen] = useState(false);
  const [position, setPosition] = useState<{ top: number; left: number; maxHeight: number } | null>(null);

  const close = useCallback((restoreFocus = false) => {
    setOpen(false);
    activeMenuCloser = null;
    if (restoreFocus) triggerRef.current?.focus();
  }, []);

  const reposition = useCallback(() => {
    const trigger = triggerRef.current;
    if (!trigger) return;
    const rect = trigger.getBoundingClientRect();
    const width = Math.min(240, window.innerWidth - 16);
    const viewportPadding = 8;
    const left = align === "end" ? rect.right - width : rect.left;
    const availableBelow = window.innerHeight - rect.bottom - 14;
    const availableAbove = rect.top - 14;
    const openAbove = availableBelow < 180 && availableAbove > availableBelow;
    const maxHeight = Math.max(160, Math.min(380, openAbove ? availableAbove : availableBelow));
    setPosition({
      top: openAbove ? Math.max(viewportPadding, rect.top - maxHeight - 6) : rect.bottom + 6,
      left: Math.min(Math.max(viewportPadding, left), window.innerWidth - width - viewportPadding),
      maxHeight,
    });
  }, [align]);

  const openMenu = useCallback(() => {
    if (activeMenuCloser && activeMenuCloser !== close) activeMenuCloser();
    activeMenuCloser = close;
    reposition();
    setOpen(true);
  }, [close, reposition]);

  useEffect(() => {
    if (!open) return;
    const frame = window.requestAnimationFrame(() => {
      menuRef.current?.querySelector<HTMLElement>('[role="menuitem"]:not([aria-disabled="true"])')?.focus();
    });

    function handlePointerDown(event: MouseEvent) {
      const target = event.target as Node;
      if (triggerRef.current?.contains(target) || menuRef.current?.contains(target)) return;
      close(false);
    }
    function handleKeyDown(event: globalThis.KeyboardEvent) {
      if (event.key !== "Escape") return;
      event.preventDefault();
      close(true);
    }
    function handleViewportChange(event: Event) {
      if (event.target instanceof Node && menuRef.current?.contains(event.target)) return;
      close(false);
    }

    document.addEventListener("mousedown", handlePointerDown);
    window.addEventListener("keydown", handleKeyDown);
    window.addEventListener("resize", handleViewportChange);
    window.addEventListener("scroll", handleViewportChange, true);
    return () => {
      window.cancelAnimationFrame(frame);
      document.removeEventListener("mousedown", handlePointerDown);
      window.removeEventListener("keydown", handleKeyDown);
      window.removeEventListener("resize", handleViewportChange);
      window.removeEventListener("scroll", handleViewportChange, true);
    };
  }, [close, open]);

  useEffect(() => {
    close(false);
  }, [close, pathname]);

  useEffect(() => () => {
    if (activeMenuCloser === close) activeMenuCloser = null;
  }, [close]);

  function handleMenuKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    const items = Array.from(
      menuRef.current?.querySelectorAll<HTMLElement>('[role="menuitem"]:not([aria-disabled="true"])') ?? [],
    );
    if (items.length === 0) return;
    const currentIndex = items.indexOf(document.activeElement as HTMLElement);
    if (event.key === "ArrowDown") {
      event.preventDefault();
      items[(currentIndex + 1) % items.length]?.focus();
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      items[(currentIndex - 1 + items.length) % items.length]?.focus();
    } else if (event.key === "Home") {
      event.preventDefault();
      items[0]?.focus();
    } else if (event.key === "End") {
      event.preventDefault();
      items[items.length - 1]?.focus();
    } else if (event.key === "Tab") {
      close(false);
    }
  }

  return (
    <>
      <button
        ref={triggerRef}
        type="button"
        onClick={() => (open ? close(false) : openMenu())}
        onKeyDown={(event) => {
          if (event.key !== "ArrowDown") return;
          event.preventDefault();
          openMenu();
        }}
        aria-label={label}
        aria-haspopup="menu"
        aria-expanded={open}
        aria-controls={open ? menuId : undefined}
        className={`rigx-row-actions-trigger ${triggerClassName}`.trim()}
      >
        <span aria-hidden="true">⋯</span>
      </button>

      {open && position && typeof document !== "undefined"
        ? createPortal(
            <div
              ref={menuRef}
              id={menuId}
              role="menu"
              aria-label={label}
              onKeyDown={handleMenuKeyDown}
              className="rigx-row-actions-menu"
              style={{ top: position.top, left: position.left, maxHeight: position.maxHeight }}
            >
              {actions.map((action) => {
                const className = `rigx-row-action${action.tone === "danger" ? " is-danger" : ""}`;
                if (action.href) {
                  return (
                    <Link
                      key={action.key}
                      href={action.href}
                      target={action.target}
                      role="menuitem"
                      aria-disabled={action.disabled ? "true" : undefined}
                      tabIndex={action.disabled ? -1 : 0}
                      onClick={() => close(false)}
                      className={className}
                    >
                      {action.icon ? <Icon name={action.icon} className="h-4 w-4" /> : null}
                      <span>{action.label}</span>
                    </Link>
                  );
                }
                return (
                  <button
                    key={action.key}
                    type="button"
                    role="menuitem"
                    disabled={action.disabled}
                    onClick={() => {
                      action.onSelect?.();
                      close(false);
                    }}
                    className={className}
                  >
                    {action.icon ? <Icon name={action.icon} className="h-4 w-4" /> : null}
                    <span>{action.label}</span>
                  </button>
                );
              })}
            </div>,
            document.body,
          )
        : null}
    </>
  );
}
