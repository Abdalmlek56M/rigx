import type { Metadata } from "next";
import { HomeContentAdmin } from "@/components/home-content-admin";

export const metadata: Metadata = {
  title: "Homepage content | RIGX Admin",
};

export default function AdminHomeContentPage() {
  return <HomeContentAdmin />;
}
