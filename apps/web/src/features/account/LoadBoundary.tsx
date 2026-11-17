"use client";

import type { ReactNode } from "react";
import { useTranslations } from "next-intl";
import type { Load } from "@/lib/hooks/use-load";

export function SkeletonRows({ count = 3 }: { count?: number }) {
  return (
    <div aria-hidden="true" className="flex flex-col gap-3">
      {Array.from({ length: count }, (_, index) => (
        <div key={index} className="h-28 animate-pulse rounded-[10px] border border-line bg-surface" />
      ))}
    </div>
  );
}

interface LoadBoundaryProps<T> {
  state: Load<T>;
  reload: () => void;
  children: (data: T) => ReactNode;
}

/** Loading rows, a retry message on failure, and `children(data)` once loaded. */
export function LoadBoundary<T>({ state, reload, children }: LoadBoundaryProps<T>) {
  const t = useTranslations("account.state");
  if (state.status === "loading") return <div role="status" aria-label={t("loading")}><SkeletonRows /></div>;
  if (state.status === "error") {
    return (
      <div role="alert" className="flex flex-col items-start gap-3 rounded-[10px] border border-line bg-surface p-6">
        <p className="m-0 font-bold">{t("failed")}</p>
        <button type="button" onClick={reload} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon">{t("retry")}</button>
      </div>
    );
  }
  return <>{children(state.data)}</>;
}

/** The empty state of a list, with an optional way on. */
export function EmptyState({ message, action }: { message: string; action?: ReactNode }) {
  return (
    <div role="status" className="flex flex-col items-center gap-3 rounded-[10px] border border-line bg-surface p-10 text-center">
      <p className="m-0 text-lg font-bold">{message}</p>
      {action}
    </div>
  );
}
