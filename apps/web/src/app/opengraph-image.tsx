import { ImageResponse } from "next/og";
import { siteOrigin } from "@/lib/seo/urls";

/** Default share image (linked from `openGraphBase`); product and build pages use their first photo instead. */
export const alt = "RIGX Hardware";
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

// Latin text only: the image renderer does not shape Arabic.
export default function OpenGraphImage() {
  const host = new URL(siteOrigin()).host;
  return new ImageResponse(
    (
      <div style={{ width: "100%", height: "100%", display: "flex", alignItems: "center", gap: 64, padding: 96, background: "#15171B", color: "#EDEFF2" }}>
        <svg width="240" height="240" viewBox="0 0 100 100">
          <polygon points="70,0 100,0 30,100 0,100" fill="#FF6A2B" />
          <polygon points="0,0 30,0 100,100 70,100" fill="#EDEFF2" />
        </svg>
        <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
          <div style={{ fontSize: 112, fontWeight: 700, letterSpacing: -2 }}>RIGX</div>
          <div style={{ fontSize: 40, color: "#FF6A2B" }}>New &amp; used PC hardware</div>
          <div style={{ fontSize: 32, color: "#9AA0A8" }}>{host}</div>
        </div>
      </div>
    ),
    size
  );
}
