import { useId } from "react";

/** RIGX wordmark with the orange X, drawn in the current text colour (ink on light, near-white on carbon). */
export function Logo({ width = 128 }: { width?: number }) {
  const maskId = useId();
  return (
    <svg width={width} height={(width * 100) / 365} viewBox="40 40 365 100" aria-hidden="true" className="shrink-0">
      <g transform="translate(40 40)">
        <g fill="currentColor">
          <g fill="none" stroke="currentColor" strokeWidth="22" strokeLinejoin="miter">
            <path d="M11 100V11H60L77 28V46L60 63H11" />
            <path d="M84 24L70 11H30L11 30V70L30 89H70L89 70V52H52" transform="translate(149 0)" />
          </g>
          <polygon points="40,63 66,63 95,100 69,100" />
          <rect x="111" y="0" width="22" height="100" />
        </g>
        <g transform="translate(265 0)">
          <mask id={maskId} maskUnits="userSpaceOnUse" x="-20" y="-20" width="140" height="140">
            <rect x="-20" y="-20" width="140" height="140" fill="#fff" />
            <polygon points="0,0 30,0 100,100 70,100" fill="#000" stroke="#000" strokeWidth="13" />
          </mask>
          <polygon points="70,0 100,0 30,100 0,100" fill="#FF6A2B" mask={`url(#${maskId})`} />
          <polygon points="0,0 30,0 100,100 70,100" fill="currentColor" />
        </g>
      </g>
    </svg>
  );
}
