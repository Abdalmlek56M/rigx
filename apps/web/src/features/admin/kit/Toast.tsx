"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { useTranslations } from "next-intl";
import { AlertIcon, CheckIcon, CloseIcon, InfoIcon, UndoIcon } from "@/components/ui/icons";
import { Button, IconButton } from "./Button";
import { dropToast, pushToast, toastDuration, type ToastItem, type ToastTone } from "./toast-queue";

type ShowToast = (message: string, tone?: ToastTone, detail?: string) => void;
/** A success toast with an "undo" button: `onUndo` runs when it is pressed (and the toast leaves). */
type ShowUndoToast = (message: string, onUndo: () => void | Promise<void>) => void;

const ToastContext = createContext<{ show: ShowToast; undo: ShowUndoToast } | null>(null);

const TONES: Record<ToastTone, { role: "status" | "alert"; icon: typeof CheckIcon; color: string }> = {
  success: { role: "status", icon: CheckIcon, color: "text-success" },
  error: { role: "alert", icon: AlertIcon, color: "text-error-text" },
  info: { role: "status", icon: InfoIcon, color: "text-orange-text" }
};

/** One toast: enters, waits five seconds (eight with an undo button; paused while hovered or focused), then leaves. */
function ToastCard({ toast, onClose }: { toast: ToastItem; onClose: () => void }) {
  const t = useTranslations("admin.kit");
  const tone = TONES[toast.tone];
  const Icon = tone.icon;
  const [entered, setEntered] = useState(false);
  const [paused, setPaused] = useState(false);
  const [leaving, setLeaving] = useState(false);
  const startedAt = useRef(0);
  const duration = toastDuration(toast);
  const left = useRef(duration);
  const bar = useRef<HTMLSpanElement>(null);

  useEffect(() => {
    const frame = window.requestAnimationFrame(() => setEntered(true));
    return () => window.cancelAnimationFrame(frame);
  }, []);

  useEffect(() => {
    if (paused || leaving) return;
    startedAt.current = Date.now();
    const timer = window.setTimeout(() => setLeaving(true), left.current);
    return () => window.clearTimeout(timer);
  }, [paused, leaving]);

  useEffect(() => {
    if (!leaving) return;
    const timer = window.setTimeout(onClose, 200);
    return () => window.clearTimeout(timer);
  }, [leaving, onClose]);

  useEffect(() => {
    const element = bar.current;
    if (!element || window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
    const animation = element.animate([{ transform: "scaleX(1)" }, { transform: "scaleX(0)" }], {
      duration,
      easing: "linear",
      fill: "forwards"
    });
    return () => animation.cancel();
  }, [duration]);

  useEffect(() => {
    const element = bar.current;
    if (!element) return;
    const animation = element.getAnimations()[0];
    if (!animation) return;
    if (paused) animation.pause();
    else animation.play();
  }, [paused]);

  function pause() {
    if (paused || leaving) return;
    left.current = Math.max(0, left.current - (Date.now() - startedAt.current));
    setPaused(true);
  }

  return (
    <div
      role={tone.role}
      onMouseEnter={pause}
      onMouseLeave={() => setPaused(false)}
      onFocusCapture={pause}
      onBlurCapture={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget as Node | null)) setPaused(false);
      }}
      className={`pointer-events-auto flex w-full items-start gap-3 rounded-card border border-line bg-surface p-4 text-ink shadow-[0_8px_28px_rgba(21,23,27,.18)] transition-[opacity,transform] duration-200 ease-out motion-reduce:transition-none ${entered && !leaving ? "translate-y-0 opacity-100" : "translate-y-2 opacity-0"}`}
    >
      <span className={`mt-0.5 ${tone.color}`}><Icon size={20} /></span>
      <div className="flex min-w-0 flex-1 flex-col gap-0.5">
        <p className="m-0 text-sm font-bold">{toast.message}</p>
        {toast.detail ? <p className="m-0 text-xs text-ink-muted">{toast.detail}</p> : null}
        {toast.action ? (
          <Button
            size="sm" variant="secondary" icon={UndoIcon} className="mt-1.5 self-start"
            onClick={() => {
              toast.action?.run();
              setLeaving(true);
            }}
          >
            {toast.action.label}
          </Button>
        ) : null}
        <span aria-hidden="true" className="mt-2 h-0.5 overflow-hidden rounded-full bg-line">
          <span ref={bar} className="block h-full origin-left bg-current rtl:origin-right motion-reduce:hidden" />
        </span>
      </div>
      <IconButton icon={CloseIcon} label={t("toasts.close")} size="sm" className="-me-1 -mt-1" onClick={() => setLeaving(true)} />
    </div>
  );
}

/**
 * The toast stack. It lives in the browser's top layer (a manual popover) and is shown again when a toast arrives, so a toast is
 * seen above an open drawer or dialog instead of behind its backdrop.
 */
function ToastRegion({ toasts, onClose }: { toasts: readonly ToastItem[]; onClose: (id: number) => void }) {
  const t = useTranslations("admin.kit");
  const region = useRef<HTMLElement>(null);

  useEffect(() => {
    const element = region.current;
    if (!element || typeof element.showPopover !== "function") return;
    if (element.matches(":popover-open")) element.hidePopover();
    element.showPopover();
  }, [toasts]);

  return (
    <section
      ref={region} popover="manual" aria-label={t("toasts.region")}
      className="pointer-events-none fixed inset-auto bottom-4 start-4 z-50 m-0 flex w-[min(380px,calc(100vw-2rem))] flex-col gap-2 overflow-visible border-0 bg-transparent p-0"
    >
      {toasts.map((toast) => <ToastCard key={toast.id} toast={toast} onClose={() => onClose(toast.id)} />)}
    </section>
  );
}

/** Success, error and info toasts after an admin action; at most three, bottom-start, five seconds each (eight with an undo button). */
export function ToastProvider({ children }: { children: ReactNode }) {
  const t = useTranslations("admin.kit");
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const nextId = useRef(0);

  const show = useCallback<ShowToast>((message, tone = "success", detail) => {
    const id = ++nextId.current;
    setToasts((list) => pushToast(list, { id, message, tone, detail }));
  }, []);

  const undo = useCallback<ShowUndoToast>((message, onUndo) => {
    const id = ++nextId.current;
    setToasts((list) => pushToast(list, { id, message, tone: "success", action: { label: t("toasts.undo"), run: () => void onUndo() } }));
  }, [t]);

  const close = useCallback((id: number) => setToasts((list) => dropToast(list, id)), []);
  const value = useMemo(() => ({ show, undo }), [show, undo]);

  return (
    <ToastContext.Provider value={value}>
      {children}
      {toasts.length > 0 && typeof document !== "undefined" ? createPortal(<ToastRegion toasts={toasts} onClose={close} />, document.body) : null}
    </ToastContext.Provider>
  );
}

function useToastContext() {
  const context = useContext(ToastContext);
  if (!context) throw new Error("The toast hooks need the admin ToastProvider.");
  return context;
}

export function useToast(): ShowToast {
  return useToastContext().show;
}

/** For an action that can be taken back: the toast carries an "undo" button that runs the callback. */
export function useUndoToast(): ShowUndoToast {
  return useToastContext().undo;
}
