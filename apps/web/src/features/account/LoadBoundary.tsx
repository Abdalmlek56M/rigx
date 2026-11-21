"use client";

import type { ComponentType, ReactNode } from "react";
import { useTranslations } from "next-intl";
import { EmptyState as KitEmptyState } from "@/components/ui/EmptyState";
import { ErrorBlock } from "@/components/ui/ErrorBlock";
import { SkeletonRows as KitSkeletonRows } from "@/components/ui/Skeleton";
import type { Load } from "@/lib/hooks/use-load";

export function SkeletonRows({ count = 3 }: { count?: number }) {
  return <KitSkeletonRows rows={count} />;
}

interface LoadBoundaryProps<T> {
  state: Load<T>;
  reload: () => void;
  children: (data: T) => ReactNode;
}

/** Loading rows, a retry card on failure, and `children(data)` once loaded. */
export function LoadBoundary<T>({ state, reload, children }: LoadBoundaryProps<T>) {
  const t = useTranslations("account.state");
  if (state.status === "loading") return <div role="status" aria-label={t("loading")}><SkeletonRows /></div>;
  if (state.status === "error") return <ErrorBlock message={t("failed")} onRetry={reload} retryLabel={t("retry")} />;
  return <>{children(state.data)}</>;
}

/** The empty state of a list, with an optional way on. */
export function EmptyState({ message, action, icon }: { message: string; action?: ReactNode; icon: ComponentType<{ size?: number }> }) {
  return <div role="status"><KitEmptyState icon={icon} title={message} action={action} /></div>;
}
