"use client";

import { useRef, type ReactNode } from "react";
import { CopyImageButton } from "./CopyImageButton";
import { ScaledPaper } from "./ScaledPaper";

interface DocumentFrameProps {
  /** The paper's design width in pixels: it is laid out at this width and scaled down to fit a narrower screen, so it looks the same everywhere. */
  width: number;
  fileKind: string;
  fileLabel: string;
  /** A button at the start of the toolbar (a way back). */
  leading?: ReactNode;
  /** Buttons next to "copy as image" (print / save PDF). */
  actions?: ReactNode;
  children: ReactNode;
}

/**
 * A printed-style document on screen: the paper keeps one fixed layout (it is also what the PNG is made from), is scaled to the screen's
 * width, centred when there is room, and the toolbar above it offers the picture and the PDF. In print the scale is dropped (see `data-document-*` in the CSS).
 */
export function DocumentFrame({ width, fileKind, fileLabel, leading, actions, children }: DocumentFrameProps) {
  const paper = useRef<HTMLDivElement>(null);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-end gap-3 print:hidden">
        {leading ? <div className="me-auto">{leading}</div> : null}
        <CopyImageButton target={paper} width={width} fileKind={fileKind} fileLabel={fileLabel} />
        {actions}
      </div>
      <ScaledPaper width={width} paperRef={paper}>{children}</ScaledPaper>
    </div>
  );
}
