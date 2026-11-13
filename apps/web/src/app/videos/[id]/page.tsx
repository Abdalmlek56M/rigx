import type { Metadata } from "next";
import { VideoPlayer } from "@/components/video-player";

export const metadata: Metadata = { title: "Video | RIGX", robots: { index: false, follow: false } };

export default async function VideoPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <VideoPlayer id={id} />;
}
