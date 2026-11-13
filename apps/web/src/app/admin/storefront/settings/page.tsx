import type { Metadata } from "next";
import { StoreSettingsAdmin } from "@/components/store-settings-admin";

export const metadata: Metadata = {
  title: "Store settings | RIGX Admin",
};

export default function AdminStoreSettingsPage() {
  return <StoreSettingsAdmin />;
}
