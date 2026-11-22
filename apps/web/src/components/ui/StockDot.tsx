/**
 * The stock state as a live dot with its word: green with a soft ring while the part can be ordered, red once it is sold out.
 * A small frosted pill, so it stays readable over any photo.
 */
export function StockDot({ available, label }: { available: boolean; label: string }) {
  return (
    <span className="inline-flex h-6 items-center gap-1.5 rounded-full bg-surface/90 px-2.5 text-xs font-bold text-ink shadow-soft backdrop-blur-sm">
      <span aria-hidden="true" className="relative flex size-2">
        {available ? <span className="absolute inline-flex size-full animate-ping rounded-full bg-success opacity-60 motion-reduce:hidden" /> : null}
        <span className={`relative inline-flex size-2 rounded-full ${available ? "bg-success" : "bg-error"}`} />
      </span>
      {label}
    </span>
  );
}
