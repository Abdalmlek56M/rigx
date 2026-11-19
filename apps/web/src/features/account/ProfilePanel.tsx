"use client";

import { useTranslations } from "next-intl";
import { useSession } from "@/features/auth/use-session";
import { AvatarEditor } from "./AvatarEditor";
import { EmailChange } from "./EmailChange";
import { LoginMethods } from "./LoginMethods";
import { NewDeviceAlerts } from "./NewDeviceAlerts";
import { ProfileForm } from "./ProfileForm";
import { SessionsList } from "./SessionsList";

const card = "flex flex-col gap-4 rounded-card border border-line bg-surface p-5 md:p-6";

/** "بياناتي والأمان": profile, picture, email, login methods, new-device alerts and signed-in devices. */
export function ProfilePanel() {
  const t = useTranslations("account.profile");
  const session = useSession();
  if (session.status !== "signedIn") return null;
  const { user } = session;

  return (
    <div className="flex flex-col gap-4">
      <h2 className="sr-only">{t("title")}</h2>
      <section className={card}>
        <AvatarEditor user={user} />
        <ProfileForm user={user} />
      </section>
      <section className={card}>
        <h3 className="m-0 text-lg font-bold">{t("security")}</h3>
        <EmailChange email={user.email} />
        <LoginMethods />
        <NewDeviceAlerts />
      </section>
      <section className={card}>
        <SessionsList />
      </section>
    </div>
  );
}
