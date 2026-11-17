"use client";

import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from "react";
import { dropToast, pushToast, TOAST_MS, type ToastItem, type ToastTone } from "./toast-queue";

type ShowToast = (message: string, tone?: ToastTone) => void;

const ToastContext = createContext<ShowToast | null>(null);

/** Short confirmations ("اتحفظ") and failures after an admin action; one region, announced politely. */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const nextId = useRef(0);
  const timers = useRef(new Set<ReturnType<typeof setTimeout>>());

  useEffect(() => {
    const pending = timers.current;
    return () => pending.forEach(clearTimeout);
  }, []);

  const show = useCallback<ShowToast>((message, tone = "success") => {
    const id = ++nextId.current;
    setToasts((list) => pushToast(list, { id, message, tone }));
    const timer = setTimeout(() => {
      timers.current.delete(timer);
      setToasts((list) => dropToast(list, id));
    }, TOAST_MS);
    timers.current.add(timer);
  }, []);

  return (
    <ToastContext.Provider value={show}>
      {children}
      <div role="status" aria-live="polite" className="pointer-events-none fixed inset-x-4 bottom-4 z-50 flex flex-col items-center gap-2 md:inset-x-auto md:end-6">
        {toasts.map((toast) => (
          <p
            key={toast.id}
            className={`m-0 rounded-card px-4 py-3 text-sm font-bold shadow-[0_6px_24px_rgba(21,23,27,.2)] ${toast.tone === "error" ? "bg-error text-surface" : "bg-carbon text-on-carbon"}`}
          >
            {toast.message}
          </p>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): ShowToast {
  const show = useContext(ToastContext);
  if (!show) throw new Error("useToast needs the admin ToastProvider.");
  return show;
}
