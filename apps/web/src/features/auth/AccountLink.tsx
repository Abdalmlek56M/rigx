"use client";

import type { ReactNode } from "react";
import { Link } from "@/i18n/navigation";
import { useSession } from "./use-session";

/** Goes to the account page when signed in and to sign-in otherwise; `children` is the icon/label. */
export function AccountLink({ className, children, ariaLabel }: { className: string; children: ReactNode; ariaLabel?: string }) {
  const session = useSession();
  const signedIn = session.status === "signedIn";
  return (
    <Link href={signedIn ? "/account" : "/login"} aria-label={ariaLabel} className={`relative ${className}`}>
      {children}
      {signedIn ? <span aria-hidden="true" className="absolute end-1.5 top-1.5 h-2 w-2 rounded-full bg-orange" /> : null}
    </Link>
  );
}
