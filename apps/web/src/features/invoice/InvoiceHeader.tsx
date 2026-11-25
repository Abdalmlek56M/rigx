import { Logo } from "@/components/ui/Logo";

/** The faint store name tilted across the invoice's background (the "background logo" setting). In print it sits on the first page. */
export function InvoiceBackgroundMark({ storeName }: { storeName: string }) {
  return (
    <span aria-hidden="true" className="pointer-events-none absolute inset-0 -z-10 flex select-none items-center justify-center overflow-hidden print:bottom-auto print:h-[270mm]">
      <span dir="ltr" className="-rotate-[28deg] whitespace-nowrap text-[96px] font-bold uppercase tracking-widest text-ink opacity-[0.04] [print-color-adjust:exact]">{storeName}</span>
    </span>
  );
}

/** The graphite band at the top of the invoice, closed by an orange rule: the logo and store name, the document title and (for a kind of sale) its kind. */
export function InvoiceHeader({ storeName, title, kindLabel }: { storeName: string; title: string; kindLabel?: string | null }) {
  return (
    <header className="flex items-center justify-between gap-6 border-b-4 border-orange bg-carbon px-8 py-5 text-on-carbon [print-color-adjust:exact]">
      <div className="flex min-w-0 flex-col gap-1.5">
        <Logo width={150} />
        <span className="text-xs opacity-80">{storeName}</span>
      </div>
      <div className="flex shrink-0 flex-col items-end gap-1 text-end">
        <span className="text-[28px] leading-tight font-bold">{title}</span>
        {kindLabel ? <span className="text-sm opacity-80">{kindLabel}</span> : null}
      </div>
    </header>
  );
}
