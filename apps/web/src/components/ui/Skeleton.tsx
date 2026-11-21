import { cardClass } from "./Card";

/** A muted block that holds the place of content while it loads (reduced motion stops the pulse). */
export function Skeleton({ className = "" }: { className?: string }) {
  return <span aria-hidden="true" className={`block animate-pulse rounded-control bg-line motion-reduce:animate-none ${className}`} />;
}

/** A card-shaped skeleton for a list that is still loading. */
export function SkeletonCard({ lines = 3, className = "" }: { lines?: number; className?: string }) {
  return (
    <div aria-hidden="true" className={cardClass({ padding: "md", className })}>
      <Skeleton className="h-4 w-2/5" />
      <div className="mt-3 flex flex-col gap-2">
        {Array.from({ length: lines }, (_, index) => (
          <Skeleton key={index} className={`h-3 ${index === lines - 1 ? "w-3/5" : "w-full"}`} />
        ))}
      </div>
    </div>
  );
}

/** A grid of card skeletons for the catalog and the builds list. */
export function SkeletonCards({ count = 6, label }: { count?: number; label: string }) {
  return (
    <div role="status" aria-label={label}>
      <ul className="m-0 grid list-none grid-cols-2 gap-3 p-0 sm:grid-cols-[repeat(auto-fill,minmax(230px,1fr))] md:gap-4">
        {Array.from({ length: count }, (_, index) => (
          <li key={index} className={`${cardClass({ padding: "none" })} overflow-hidden`}>
            <Skeleton className="aspect-square w-full rounded-none" />
            <div className="flex flex-col gap-2 p-4">
              <Skeleton className="h-3.5 w-4/5" />
              <Skeleton className="h-3 w-1/2" />
              <Skeleton className="mt-2 h-9 w-full rounded-full" />
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}

/** The loading rows of an account list or a panel. */
export function SkeletonRows({ rows = 3 }: { rows?: number }) {
  return (
    <div aria-hidden="true" className="flex flex-col gap-3">
      {Array.from({ length: rows }, (_, index) => (
        <SkeletonCard key={index} lines={2} />
      ))}
    </div>
  );
}
