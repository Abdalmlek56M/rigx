/** How a blacklist entry is shown: what identifies it, with the phone partly hidden. Pure, so it is tested. */
import type { BlacklistEntry } from "./orders-api";

/** "01012345678" → "010•••••678"; a short number is shown as it is. */
export function maskPhone(phone: string): string {
  const digits = phone.replace(/\s/g, "");
  return digits.length >= 8 ? `${digits.slice(0, 3)}${"•".repeat(digits.length - 6)}${digits.slice(-3)}` : digits;
}

/** The entry's name, else its email, else its address, else null (the page then says "customer"). */
export function blacklistTitle(entry: Pick<BlacklistEntry, "name" | "email" | "addressText">): string | null {
  return entry.name || entry.email || entry.addressText || null;
}
