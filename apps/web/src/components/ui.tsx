"use client";

import Link from "next/link";
import type { ButtonHTMLAttributes, InputHTMLAttributes, ReactNode } from "react";
import { useEffect, useId, useRef, useState } from "react";
import { useLocale } from "@/lib/i18n";
import { Icon } from "@/components/icons";

export function PageIntro({
  eyebrow,
  title,
  lead,
}: {
  eyebrow: string;
  title: string;
  lead: string;
}) {
  return (
    <header className="rigx-page-intro mb-7">
      <p className="mb-2 text-xs font-bold tracking-[0.18em] text-[var(--ds-accent)] uppercase" dir="ltr">
        {eyebrow}
      </p>
      <h1 dir="auto" className="text-balance text-3xl font-bold tracking-tight text-[var(--ds-text)] sm:text-4xl">{title}</h1>
      <p dir="auto" className="mt-3 max-w-2xl text-base leading-7 text-[var(--ds-text-muted)]">{lead}</p>
    </header>
  );
}

export function AuthPanel({
  children,
  aside,
}: {
  children: ReactNode;
  aside?: ReactNode;
}) {
  return (
    <main className={`auth-page${aside ? " has-aside" : ""}`}>
      <section className="auth-card glass-panel">
        {children}
      </section>
      {aside ? (
        <aside className="auth-aside glass-panel">
          {aside}
        </aside>
      ) : null}
    </main>
  );
}

export function FormStack({ children }: { children: ReactNode }) {
  return <div className="grid gap-5">{children}</div>;
}

export function TextField({
  label,
  hint,
  error,
  className = "",
  ...props
}: InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  hint?: string;
  error?: string;
}) {
  const generatedId = useId();
  const id = props.id ?? generatedId;
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;

  return (
    <div className={className}>
      <label className="mb-2 block text-sm font-semibold text-[var(--ds-text)]" htmlFor={id}>
        {label}
      </label>
      <input
        {...props}
        id={id}
        aria-describedby={[hintId, errorId].filter(Boolean).join(" ") || undefined}
        aria-invalid={Boolean(error)}
        className="min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-4 py-2.5 text-[var(--ds-text)] outline-none transition focus:border-[var(--ds-accent)] focus:ring-4 focus:ring-[var(--ds-accent)]/15 disabled:cursor-not-allowed disabled:bg-[var(--ds-surface-muted)]"
      />
      {hint ? <p id={hintId} className="mt-2 text-sm leading-6 text-[var(--ds-text-muted)]">{hint}</p> : null}
      {error ? <p id={errorId} className="mt-2 text-sm font-medium text-[var(--ds-danger)]">{error}</p> : null}
    </div>
  );
}

export function PasswordField({
  label,
  hint,
  error,
  ...props
}: InputHTMLAttributes<HTMLInputElement> & {
  label: string;
  hint?: string;
  error?: string;
}) {
  const { messages } = useLocale();
  const [visible, setVisible] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const restoreFocus = useRef(false);
  const selection = useRef<{ start: number | null; end: number | null }>({ start: null, end: null });
  const generatedId = useId();
  const id = props.id ?? generatedId;
  const describedBy = hint || error ? `${id}-description` : undefined;

  function toggleVisibility() {
    const input = inputRef.current;
    selection.current = {
      start: input?.selectionStart ?? null,
      end: input?.selectionEnd ?? null,
    };
    restoreFocus.current = true;
    setVisible((current) => !current);
  }

  useEffect(() => {
    if (!restoreFocus.current) return;
    const input = inputRef.current;
    input?.focus();
    if (input && selection.current.start !== null && selection.current.end !== null) {
      input.setSelectionRange(selection.current.start, selection.current.end);
    }
    restoreFocus.current = false;
  }, [visible]);

  return (
    <div>
      <label className="mb-2 block text-sm font-semibold text-[var(--ds-text)]" htmlFor={id}>
        {label}
      </label>
      <div className="relative">
        <input
          {...props}
          ref={inputRef}
          id={id}
          type={visible ? "text" : "password"}
          aria-describedby={describedBy}
          aria-invalid={Boolean(error)}
          className="min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-4 py-2.5 pe-14 text-[var(--ds-text)] outline-none transition focus:border-[var(--ds-accent)] focus:ring-4 focus:ring-[var(--ds-accent)]/15"
        />
        <button
          type="button"
          onClick={toggleVisibility}
          disabled={props.disabled}
          aria-label={visible ? messages.hidePassword : messages.showPassword}
          aria-pressed={visible}
          title={visible ? messages.hidePassword : messages.showPassword}
          className="absolute inset-y-1 end-1 grid min-h-10 min-w-10 place-items-center rounded-lg text-[var(--ds-text-muted)] transition hover:bg-[var(--ds-surface-muted)] hover:text-[var(--ds-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--ds-accent)] disabled:pointer-events-none disabled:opacity-50"
        >
          <Icon name={visible ? "eyeOff" : "eye"} />
        </button>
      </div>
      {error || hint ? (
        <p id={`${id}-description`} className={`mt-2 text-sm leading-6 ${error ? "font-medium text-[var(--ds-danger)]" : "text-[var(--ds-text-muted)]"}`}>
          {error ?? hint}
        </p>
      ) : null}
    </div>
  );
}

export function PrimaryButton({
  children,
  pending = false,
  className = "",
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & {
  pending?: boolean;
}) {
  return (
    <button
      {...props}
      disabled={props.disabled || pending}
      className={`inline-flex min-h-11 items-center justify-center gap-2 rounded-xl bg-[var(--ds-accent-strong)] px-5 py-2.5 font-semibold text-[var(--ds-on-accent)] shadow-sm transition hover:bg-[var(--ds-accent)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--ds-accent)] disabled:cursor-not-allowed disabled:opacity-60 ${className}`}
    >
      {pending ? <span className="h-4 w-4 animate-spin rounded-full border-2 border-white/40 border-t-white" aria-hidden /> : null}
      {children}
    </button>
  );
}

export function SecondaryButton({
  children,
  className = "",
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      {...props}
      className={`inline-flex min-h-11 items-center justify-center rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-5 py-2.5 font-semibold text-[var(--ds-text)] transition hover:border-[var(--ds-accent)] hover:bg-[var(--ds-surface-muted)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--ds-accent)] disabled:cursor-not-allowed disabled:opacity-60 ${className}`}
    >
      {children}
    </button>
  );
}

export function Message({
  tone,
  children,
}: {
  tone: "success" | "error" | "info";
  children: ReactNode;
}) {
  const styles = {
    success: "border-emerald-200 bg-emerald-50 text-emerald-900",
    error: "border-red-200 bg-red-50 text-red-900",
    info: "border-[var(--ds-border)] bg-[var(--ds-accent-soft)] text-[var(--ds-accent-strong)]",
  };

  return (
    <div
      role={tone === "error" ? "alert" : "status"}
      className={`rounded-xl border px-4 py-3 text-sm leading-6 ${styles[tone]}`}
    >
      {children}
    </div>
  );
}

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel,
  cancelLabel,
  pending = false,
  onConfirm,
  onCancel,
}: {
  open: boolean;
  title: string;
  description: string;
  confirmLabel: string;
  cancelLabel: string;
  pending?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  const titleId = useId();
  const descriptionId = useId();
  const dialogRef = useRef<HTMLElement>(null);

  useEffect(() => {
    if (!open) return;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    dialogRef.current?.focus();
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !pending) onCancel();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [onCancel, open, pending]);

  if (!open) return null;
  return (
    <div className="fixed inset-0 z-[70] grid place-items-center px-4" role="alertdialog" aria-modal="true" aria-labelledby={titleId} aria-describedby={descriptionId}>
      <button type="button" tabIndex={-1} aria-label={cancelLabel} onClick={() => !pending && onCancel()} className="absolute inset-0 bg-slate-950/65 backdrop-blur-sm" />
      <section ref={dialogRef} tabIndex={-1} className="ds-surface relative w-full max-w-md rounded-2xl p-6 outline-none">
        <h2 id={titleId} className="text-xl font-black text-[var(--ds-text)]">{title}</h2>
        <p id={descriptionId} className="mt-3 text-sm leading-7 text-[var(--ds-text-muted)]">{description}</p>
        <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <SecondaryButton type="button" onClick={onCancel} disabled={pending}>{cancelLabel}</SecondaryButton>
          <PrimaryButton type="button" onClick={onConfirm} pending={pending}>{confirmLabel}</PrimaryButton>
        </div>
      </section>
    </div>
  );
}

export function SideDrawer({
  open,
  title,
  closeLabel = title,
  onClose,
  children,
}: {
  open: boolean;
  title: string;
  closeLabel?: string;
  onClose: () => void;
  children: ReactNode;
}) {
  const titleId = useId();
  const panelRef = useRef<HTMLElement>(null);
  const closeButtonRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) return;
    const previousOverflow = document.body.style.overflow;
    const previousFocus = document.activeElement as HTMLElement | null;
    document.body.style.overflow = "hidden";
    closeButtonRef.current?.focus();
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
      if (event.key !== "Tab" || !panelRef.current) return;
      const focusable = Array.from(panelRef.current.querySelectorAll<HTMLElement>(
        'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'));
      if (focusable.length === 0) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", onKeyDown);
      previousFocus?.focus();
    };
  }, [onClose, open]);

  if (!open) return null;
  return (
    <div className="fixed inset-0 z-[75]">
      <button type="button" tabIndex={-1} aria-label={closeLabel} onClick={onClose} className="absolute inset-0 bg-slate-950/65 backdrop-blur-sm" />
      <section
        ref={panelRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="ds-surface absolute inset-y-0 end-0 w-[min(32rem,94vw)] overflow-y-auto p-5 shadow-2xl sm:p-7"
      >
        <div className="mb-6 flex items-center justify-between gap-4">
          <h2 id={titleId} className="text-xl font-black text-[var(--ds-text)]">{title}</h2>
          <button
            ref={closeButtonRef}
            type="button"
            onClick={onClose}
            aria-label={closeLabel}
            className="grid min-h-10 min-w-10 place-items-center rounded-xl border border-[var(--ds-border)] text-[var(--ds-text-muted)] hover:bg-[var(--ds-surface-muted)] hover:text-[var(--ds-text)] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--ds-accent)]"
          >
            <Icon name="close" />
          </button>
        </div>
        {children}
      </section>
    </div>
  );
}

export function TextLink({ href, children }: { href: string; children: ReactNode }) {
  return (
    <Link
      href={href}
      className="font-semibold text-[var(--ds-accent-strong)] underline decoration-[var(--ds-accent)]/30 underline-offset-4 transition hover:text-[var(--ds-accent)]"
    >
      {children}
    </Link>
  );
}

export function LoadingCard() {
  const { messages } = useLocale();
  return (
    <div className="mx-auto w-full max-w-5xl px-5 py-16 sm:px-8" aria-live="polite">
      <div className="animate-pulse rounded-3xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-8">
        <div className="h-4 w-28 rounded bg-[var(--ds-surface-muted)]" />
        <div className="mt-5 h-10 w-2/3 rounded bg-[var(--ds-surface-muted)]" />
        <p className="sr-only">{messages.loading}</p>
      </div>
    </div>
  );
}
