import type { Metadata } from "next";
import { PcBuilderWizard } from "@/components/pc-builder-wizard";

export const metadata: Metadata = { title: "تجميع حاسب | RIGX" };

export default async function PcBuilderDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <PcBuilderWizard id={id} />;
}
