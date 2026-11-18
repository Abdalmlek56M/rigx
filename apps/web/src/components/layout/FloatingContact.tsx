"use client";

import { useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { CloseIcon, DocumentIcon, WhatsAppIcon, WrenchIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { Platform, contactHref } from "@/lib/contact";
import { useDismiss } from "@/lib/hooks/use-dismiss";
import type { ContactLink } from "@/lib/storefront";
import { PlatformIcon } from "./PlatformIcon";

const itemClass = "flex min-h-11 items-center gap-2.5 rounded-card px-2.5 text-sm hover:bg-paper";

type SocialPlatform = "WhatsApp" | "Telegram" | "Facebook" | "Instagram" | "TikTok" | "YouTube";

/** Floating contact button (bottom, end side) opening a menu of the links the owner chose for it. */
export function FloatingContact({ links }: { links: ContactLink[] }) {
  const t = useTranslations("floatingContact");
  const tf = useTranslations("footer.platforms");
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);
  const close = () => setOpen(false);
  useDismiss(ref, open, close);

  const label = (platform: string) => (platform === Platform.Phone ? t("call") : tf(platform as SocialPlatform));

  return (
    <div ref={ref} className="floating-lift fixed bottom-[max(1.25rem,env(safe-area-inset-bottom))] end-4 z-30 flex flex-col items-end gap-2.5 md:bottom-6 md:end-6">
      {open ? (
        <div id="floating-contact-menu" className="pop-up flex w-[220px] flex-col gap-1 rounded-panel border border-line bg-surface p-2.5 shadow-[0_10px_30px_rgba(0,0,0,.15)]">
          {links.map((link) => (
            <a key={link.platform} href={contactHref(link)} className={itemClass} onClick={close}>
              <PlatformIcon platform={link.platform} size={18} />
              {label(link.platform)}
            </a>
          ))}
          <Link href="/support" className={itemClass} onClick={close}>
            <WrenchIcon size={18} />
            {t("support")}
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
        className="flex h-14 w-14 items-center justify-center rounded-full bg-carbon text-on-carbon shadow-[0_8px_24px_rgba(0,0,0,.25)]"
      >
        {open ? <CloseIcon size={22} /> : <WhatsAppIcon size={26} />}
      </button>
    </div>
  );
}
