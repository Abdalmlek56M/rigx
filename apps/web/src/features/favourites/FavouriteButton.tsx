"use client";

import { useState, useSyncExternalStore } from "react";
import { useTranslations } from "next-intl";
import { IconButton } from "@/components/ui/Button";
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

  const Heart = ({ size }: { size?: number }) => <HeartIcon size={size} filled={saved} />;

  return (
    <IconButton
      icon={Heart}
      label={t(saved ? "remove" : "add", { item: modelName })}
      variant="secondary"
      aria-pressed={saved}
      onClick={toggle}
      className={`shadow-soft ${saved ? "text-orange-text" : "text-ink-muted"} ${failed ? "ring-2 ring-error" : ""} ${className}`}
    />
  );
}
