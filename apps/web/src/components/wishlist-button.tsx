"use client";

import { Icon } from "@/components/icons";
import { useLocale } from "@/lib/i18n";
import { useWishlist, type WishlistProductInput } from "@/components/wishlist-provider";

export function WishlistButton({
  product,
  compact = false,
  withLabel = false,
  className = "",
}: {
  product: WishlistProductInput;
  compact?: boolean;
  withLabel?: boolean;
  className?: string;
}) {
  const { messages } = useLocale();
  const { isSaved, pendingIds, toggle } = useWishlist();
  const saved = isSaved(product.id);
  const pending = pendingIds.has(product.id);
  const label = saved ? messages.wishlistRemoveAction : messages.wishlistAddAction;

  return (
    <button
      type="button"
      onClick={() => void toggle(product)}
      aria-pressed={saved}
      aria-label={label}
      title={label}
      disabled={pending}
      className={`inline-flex min-h-10 items-center justify-center gap-2 rounded-xl border px-3 text-sm font-semibold transition focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--ds-accent)] disabled:opacity-60 ${
        saved
          ? "border-[var(--ds-accent)] bg-[var(--ds-accent-soft)] text-[var(--ds-accent-strong)]"
          : "border-[var(--ds-border)] bg-[var(--ds-surface)] text-[var(--ds-text)] hover:border-[var(--ds-accent)] hover:bg-[var(--ds-accent-soft)] hover:text-[var(--ds-accent-strong)]"
      } ${className}`.trim()}
    >
      <Icon name="heart" className="h-4 w-4" fill={saved ? "currentColor" : "none"} />
      {withLabel || !compact ? <span>{label}</span> : null}
    </button>
  );
}
