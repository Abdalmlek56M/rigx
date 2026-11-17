"use client";

import { useSession } from "@/features/auth/use-session";

const NONE: readonly string[] = [];

/** The signed-in staff member's permissions (empty while loading); use it to hide actions the API would refuse. */
export function usePermissions(): { has: (permission: string) => boolean; permissions: readonly string[] } {
  const session = useSession();
  const permissions = session.status === "signedIn" ? session.user.permissions : NONE;
  return { has: (permission) => permissions.includes(permission), permissions };
}
