import { notFound } from "next/navigation";

/** Unknown admin paths show the admin not-found page inside the admin frame (not the storefront's). */
export default function AdminCatchAll() {
  notFound();
}
