"use client";

import { useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { CloseIcon, DocumentIcon, ShieldIcon, WhatsAppIcon } from "@/components/ui/icons";
import { Link, usePathname } from "@/i18n/navigation";
import { Platform, contactLinkProps, distinctPhone } from "@/lib/contact";
import { useDismiss } from "@/lib/hooks/use-dismiss";
import type { ContactLink } from "@/lib/storefront";
import { PlatformIcon } from "./PlatformIcon";

const itemClass = "flex min-h-11 items-center gap-2.5 rounded-card px-2.5 text-sm transition-colors hover:bg-paper";

type SocialPlatform = "WhatsApp" | "Telegram" | "Facebook" | "Instagram" | "TikTok" | "YouTube";

/** Floating WhatsApp-green contact button (bottom, end side; bottom-left in Arabic) opening a menu of the links the owner chose for it. */
export function FloatingContact({ links }: { links: ContactLink[] }) {
  const t = useTranslations("floatingContact");
  const tf = useTranslations("footer.platforms");
  const [open, setOpen] = useState(false);
  // A new page closes the menu (React's "adjust state while rendering" pattern, no effect needed).
  const pathname = usePathname();
  const [shownOn, setShownOn] = useState(pathname);
  if (shownOn !== pathname) {
    setShownOn(pathname);
    setOpen(false);
  }
  const ref = useRef<HTMLDivElement>(null);
  const close = () => setOpen(false);
  useDismiss(ref, open, close);

  const phone = distinctPhone(links);
  const shown = links.filter((link) => link.platform !== Platform.Phone || link === phone);
  const label = (platform: string) => (platform === Platform.Phone ? t("call") : tf(platform as SocialPlatform));

  return (
    <div ref={ref} className="floating-lift floating-contact fixed bottom-[max(1.25rem,env(safe-area-inset-bottom))] end-4 z-30 flex flex-col items-end gap-2.5 md:bottom-6 md:end-6">
      {open ? (
        <div id="floating-contact-menu" className="pop-up flex w-[220px] flex-col gap-1 rounded-panel border border-line bg-surface p-2.5 shadow-lift">
          {shown.map((link) => (
            <a key={link.platform} {...contactLinkProps(link)} className={itemClass} onClick={close}>
              <PlatformIcon platform={link.platform} size={18} />
              {label(link.platform)}
            </a>
          ))}
          <Link href="/warranty" className={itemClass} onClick={close}>
            <ShieldIcon size={18} />
            {t("warranty")}
          </Link>
          <Link href="/faq" className={itemClass} onClick={close}>
            <DocumentIcon size={18} />
            {t("faq")}
          </Link>
        </div>
      ) : null}
      <button
        type="button"
        aria-label={t("label")}
        aria-expanded={open}
        aria-controls="floating-contact-menu"
        onClick={() => setOpen((value) => !value)}
        className={`wa-fab relative flex h-14 w-14 items-center justify-center rounded-full bg-whatsapp text-white shadow-[0_10px_28px_-6px_rgba(31,168,85,.55)] transition-[transform,box-shadow] duration-200 hover:shadow-[0_14px_32px_-6px_rgba(31,168,85,.7)] ${open ? "" : "wa-pulse"}`}
      >
        <span key={open ? "close" : "open"} className="icon-swap flex">{open ? <CloseIcon size={22} /> : <WhatsAppIcon size={28} />}</span>
      </button>
    </div>
  );
}
