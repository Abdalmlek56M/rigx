"use client";

import { useState, useSyncExternalStore } from "react";
import { useTranslations } from "next-intl";
import { HeartIcon } from "@/components/ui/icons";
import { favouriteStore } from "./favourite-store";

/** The heart on a product card or page: pressed when the item is in the favourites; a failed save flips back and says so. */
export function FavouriteButton({ code, modelName, className = "" }: { code: string; modelName: string; className?: string }) {
  const t = useTranslations("favourites");
  const codes = useSyncExternalStore(favouriteStore.subscribe, favouriteStore.getSnapshot, favouriteStore.getServerSnapshot);
  const [failed, setFailed] = useState(false);
  const saved = codes.includes(code);

  function toggle() {
    setFailed(false);
    favouriteStore.toggle(code).catch(() => setFailed(true));
  }

  return (
    <button
      type="button"
      aria-pressed={saved}
      aria-label={t(saved ? "remove" : "add", { item: modelName })}
      title={failed ? t("failed") : undefined}
      onClick={toggle}
      className={`press flex h-10 w-10 items-center justify-center rounded-full border border-line bg-surface shadow-soft transition-colors ${saved ? "text-orange-text" : "text-ink-muted hover:text-orange-text"} ${failed ? "ring-2 ring-error" : ""} ${className}`}
    >
      <HeartIcon filled={saved} />
    </button>
  );
}
