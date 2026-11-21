/** The small orange count on a header control (cart, notifications); the exact count belongs in the control's label. */
export function CountBadge({ children, className = "" }: { children: string | number; className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={`badge-pop absolute -top-0.5 end-0 flex h-5 min-w-5 items-center justify-center rounded-full accent-strong px-1.5 font-mono text-[11px] font-semibold leading-none ring-2 ring-paper ${className}`}
    >
      {children}
    </span>
  );
}
