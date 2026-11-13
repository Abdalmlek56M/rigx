import type { Metadata } from "next";
import { EmailChangeConfirm } from "@/components/email-change-confirm";

export const metadata: Metadata = { title: "Confirm email change | RIGX", robots: { index: false, follow: false } };

export default function ChangeEmailPage() {
  return <EmailChangeConfirm />;
}
