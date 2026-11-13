"use client";

import { type FormEvent, useState } from "react";
import { submitContactMessage } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useStoreSettings } from "@/components/store-settings-provider";
import { Icon } from "@/components/icons";

type FormState = { fullName: string; email: string; phone: string; subject: string; message: string; website: string };

const emptyForm: FormState = { fullName: "", email: "", phone: "", subject: "", message: "", website: "" };

export function ContactExperience() {
  const { locale, messages } = useLocale();
  const isArabic = locale === "ar";
  const { settings } = useStoreSettings();
  const [form, setForm] = useState<FormState>(emptyForm);
  const [sending, setSending] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!form.fullName.trim() || !form.email.trim() || !form.subject.trim() || !form.message.trim()) {
      setError(isArabic ? "الاسم والبريد والموضوع والرسالة مطلوبة." : "Name, email, subject, and message are required.");
      return;
    }
    setSending(true);
    setError(null);
    try {
      await submitContactMessage({
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        phone: form.phone.trim() || null,
        subject: form.subject.trim(),
        message: form.message.trim(),
        website: form.website,
      });
      setSent(true);
      setForm(emptyForm);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setSending(false);
    }
  }

  return (
    <>
      <div className="page-hero compact-hero">
        <div>
          <span className="eyebrow">RIGX / CONTACT</span>
          <h1>{isArabic ? <>تواصل <em>معنا.</em></> : <>Talk to <em>RIGX.</em></>}</h1>
          <p>
            {isArabic
              ? "اسأل عن التوافق، أو اطلب توفير قطعة، أو تابع طلبك. الرسائل تُسجل في صندوق RIGX وتُقرأ من الفريق."
              : "Ask about compatibility, request a part, or follow up on an order. Messages are recorded in the RIGX inbox and read by the team."}
          </p>
        </div>
        <div className="hero-side-stats">
          <span><b>01</b><small>{isArabic ? "أرسل الرسالة" : "Send"}</small></span>
          <span><b>02</b><small>{isArabic ? "نتابع معك" : "We reply"}</small></span>
        </div>
      </div>

      <div className="contact-layout">
        <form className="contact-form glass-panel" onSubmit={submit} noValidate>
          <div className="panel-title">
            <div>
              <span className="eyebrow">MESSAGE / RIGX</span>
              <h2>{isArabic ? "أرسل رسالة" : "Send a message"}</h2>
            </div>
          </div>
          <div className="form-grid">
            <label>{isArabic ? "الاسم الكامل" : "Full name"}
              <input value={form.fullName} onChange={(event) => setForm({ ...form, fullName: event.target.value })} autoComplete="name" required />
            </label>
            <label>{isArabic ? "البريد الإلكتروني" : "Email"}
              <input type="email" dir="ltr" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} autoComplete="email" required />
            </label>
            <label>{isArabic ? "الهاتف (اختياري)" : "Phone (optional)"}
              <input type="tel" dir="ltr" value={form.phone} onChange={(event) => setForm({ ...form, phone: event.target.value })} autoComplete="tel" />
            </label>
            <label>{isArabic ? "الموضوع" : "Subject"}
              <input value={form.subject} onChange={(event) => setForm({ ...form, subject: event.target.value })} required />
            </label>
            <label className="contact-message-field">{isArabic ? "الرسالة" : "Message"}
              <textarea rows={6} value={form.message} onChange={(event) => setForm({ ...form, message: event.target.value })} required />
            </label>
          </div>
          <label className="contact-honeypot" aria-hidden="true">
            Website
            <input tabIndex={-1} autoComplete="off" value={form.website} onChange={(event) => setForm({ ...form, website: event.target.value })} />
          </label>
          {error ? <p className="rigx-field-error" role="alert">{error}</p> : null}
          {sent ? (
            <p className="contact-success" role="status">
              <Icon name="check" /> {isArabic ? "وصلت رسالتك إلى صندوق RIGX. سنتواصل معك على البريد الذي أدخلته." : "Your message reached the RIGX inbox. We will reply to the email you provided."}
            </p>
          ) : null}
          <button type="submit" className="btn-primary" disabled={sending}>
            {sending ? (isArabic ? "جارٍ الإرسال..." : "Sending...") : (isArabic ? "إرسال الرسالة" : "Send message")} ‹
          </button>
          <p className="summary-disclaimer">
            {isArabic
              ? "لا تشارك كلمات المرور أو بيانات دفع. لا نستخدم الرسائل لإنشاء طلبات تلقائيًا."
              : "Do not share passwords or payment data. Messages never create orders automatically."}
          </p>
        </form>

        <aside className="contact-info glass-panel">
          <span className="eyebrow">RIGX / SUPPORT</span>
          <h2>{isArabic ? "قنوات التواصل" : "Contact channels"}</h2>
          {settings?.whatsAppNumber ? (
            <a className="contact-channel" href={`https://wa.me/${settings.whatsAppNumber.replace(/[^\d]/g, "")}`} target="_blank" rel="noreferrer">
              <span className="contact-channel-icon"><Icon name="whatsapp" /></span>
              <span><b>WhatsApp</b><small dir="ltr">{settings.whatsAppNumber}</small></span>
            </a>
          ) : null}
          {settings?.pickupAddress ? (
            <div className="contact-channel">
              <span className="contact-channel-icon"><Icon name="store" /></span>
              <span><b>{isArabic ? "الاستلام بالاتفاق" : "Pickup by arrangement"}</b><small>{settings.pickupAddress}</small></span>
            </div>
          ) : null}
          {settings?.pickupHours ? (
            <div className="contact-channel">
              <span className="contact-channel-icon"><Icon name="receipt" /></span>
              <span><b>{isArabic ? "مواعيد التنسيق" : "Coordination hours"}</b><small>{settings.pickupHours}</small></span>
            </div>
          ) : null}
          <p className="inline-note">
            {isArabic
              ? "طلبات الاستلام تُنسق مباشرة، ولا يُنشر عنوان داخلي. لا توجد وسيلة دفع عامة لطلبات الاستلام."
              : "Pickup orders are coordinated directly; a private address is not published. Pickup orders show no public payment method."}
          </p>
        </aside>
      </div>
    </>
  );
}
