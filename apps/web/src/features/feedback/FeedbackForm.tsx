"use client";

import { useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { Button, ButtonLink } from "@/components/ui/Button";
import { Card, cardClass } from "@/components/ui/Card";
import { ChoiceRadioTile } from "@/components/ui/ChoiceTile";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { AlertIcon, CheckIcon, InboxIcon, PencilIcon, StarIcon } from "@/components/ui/icons";
import { loginHref } from "@/features/auth/auth-rules";
import { useSession } from "@/features/auth/use-session";
import { usePathname } from "@/i18n/navigation";
import { submitRequest, type SubmitOutcome } from "@/features/requests/submit-request";
import { blankToNull, feedbackProblems, FEEDBACK_KINDS, MAX_MESSAGE, MAX_SUBJECT, type FeedbackField, type FeedbackKind } from "./feedback-rules";

const KIND_ICONS = { Complaint: AlertIcon, Suggestion: StarIcon } as const;

/**
 * One form for a complaint or a suggestion. It lands in the admin inbox; the store's written answer shows in the customer's account
 * (under "my own requests"), so a signed-in visitor gets their name, phone and email filled in.
 */
export function FeedbackForm() {
  const t = useTranslations("feedback");
  const session = useSession();
  const pathname = usePathname();
  const user = session.status === "signedIn" ? session.user : null;
  const [kind, setKind] = useState<FeedbackKind>("Complaint");
  // A field the visitor has not touched shows the account's value; touching it takes over.
  const [name, setName] = useState<string | null>(null);
  const [phone, setPhone] = useState<string | null>(null);
  const [email, setEmail] = useState<string | null>(null);
  const [subject, setSubject] = useState("");
  const [reference, setReference] = useState("");
  const [message, setMessage] = useState("");
  const [problems, setProblems] = useState<FeedbackField[]>([]);
  const [busy, setBusy] = useState(false);
  const [outcome, setOutcome] = useState<SubmitOutcome | null>(null);

  const values = { name: name ?? user?.fullName ?? "", phone: phone ?? user?.phoneNumber ?? "", email: email ?? user?.email ?? "", message };
  const fieldError = (field: FeedbackField) => (problems.includes(field) ? t(`errors.${field}`) : null);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const found = feedbackProblems(values);
    setProblems(found);
    if (found.length > 0) {
      const form = event.currentTarget;
      requestAnimationFrame(() => form.querySelector<HTMLElement>("[aria-invalid=true]")?.focus());
      return;
    }
    setBusy(true);
    setOutcome(
      await submitRequest(kind, {
        name: values.name.trim(),
        phone: values.phone.trim(),
        email: blankToNull(values.email),
        subject: blankToNull(subject),
        itemCode: blankToNull(reference),
        message: values.message.trim()
      })
    );
    setBusy(false);
  }

  if (outcome?.status === "sent") {
    return (
      <Card padding="lg" className="mx-auto w-full max-w-[640px] gap-3" icon={InboxIcon} title={t("sent.title")}>
        <StatusBadge tone="success" icon={CheckIcon}>{t(`sent.${kind}`)}</StatusBadge>
        {outcome.code ? <p className="m-0 text-sm">{t("sent.code")} <span dir="ltr" className="font-mono font-bold text-orange-text">{outcome.code}</span></p> : null}
        <p className="m-0 text-sm leading-7 text-ink-muted">{t(user ? "sent.replyInAccount" : "sent.replyByPhone")}</p>
        {user ? (
          <ButtonLink href="/account/requests" variant="secondary" size="sm" className="self-start">{t("sent.open")}</ButtonLink>
        ) : (
          <ButtonLink href={loginHref(pathname)} variant="secondary" size="sm" className="self-start">{t("sent.signIn")}</ButtonLink>
        )}
      </Card>
    );
  }

  return (
    <form
      aria-label={t("label")}
      onSubmit={submit}
      noValidate
      className={`${cardClass({ padding: "lg", layout: "grid" })} mx-auto w-full max-w-[640px] grid-cols-1 gap-4 sm:grid-cols-2`}
    >
      <fieldset className="m-0 grid grid-cols-2 gap-3 border-0 p-0 sm:col-span-2">
        <legend className="mb-2 text-xs font-bold">{t("kind")}</legend>
        {FEEDBACK_KINDS.map((value) => (
          <ChoiceRadioTile
            key={value} name="kind" value={value} icon={KIND_ICONS[value]} label={t(`kinds.${value}`)} note={t(`kindNotes.${value}`)}
            selected={kind === value} onSelect={() => setKind(value)}
          />
        ))}
      </fieldset>
      <TextField label={t("name")} name="name" value={values.name} onChange={setName} autoComplete="name" required error={fieldError("name")} />
      <TextField
        label={t("phone")} name="phone" type="tel" ltr autoComplete="tel" placeholder="01xxxxxxxxx" required
        value={values.phone} onChange={setPhone} error={fieldError("phone")}
      />
      <div className="sm:col-span-2">
        <TextField
          label={t("email")} name="email" type="email" ltr autoComplete="email" value={values.email} onChange={setEmail} error={fieldError("email")}
        />
      </div>
      <div className="sm:col-span-2">
        <TextField label={t("subject")} name="subject" value={subject} onChange={setSubject} maxLength={MAX_SUBJECT} />
      </div>
      <div className="sm:col-span-2">
        <TextField label={t("reference")} name="reference" ltr value={reference} onChange={setReference} maxLength={40} />
      </div>
      <div className="sm:col-span-2">
        <TextAreaField
          label={t("message")} name="message" rows={6} required maxLength={MAX_MESSAGE} value={message} onChange={setMessage}
          hint={t("messageHint")} error={fieldError("message")}
        />
      </div>
      {outcome ? <p role="alert" className="m-0 text-sm text-error-text sm:col-span-2">{t(outcome.status === "invalid" ? "errors.invalid" : "errors.failed")}</p> : null}
      <Button type="submit" variant="accent" icon={PencilIcon} loading={busy} className="w-full sm:col-span-2">{t("submit")}</Button>
    </form>
  );
}
