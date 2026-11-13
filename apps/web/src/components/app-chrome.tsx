"use client";

import type { ReactNode } from "react";
import { useEffect, useRef } from "react";
import { usePathname } from "next/navigation";
import { RigxHeader } from "@/components/rigx-header";
import { RigxFooter } from "@/components/rigx-footer";
import { CartDrawer } from "@/components/cart-drawer";
import { CartToast } from "@/components/cart-toast";
import { WhatsAppFloatingButton } from "@/components/whatsapp-floating-button";
import { BackToTopButton } from "@/components/back-to-top";

export function AppChrome({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const isAdmin = pathname?.startsWith("/admin") ?? false;
  const visualEngineRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (isAdmin) return;
    const engine = visualEngineRef.current;
    if (!engine) return;
    const target = engine as HTMLDivElement;

    const finePointer = window.matchMedia("(hover: hover) and (pointer: fine)");
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    if (!finePointer.matches || reducedMotion.matches) return;

    let animationFrame = 0;
    let pointerX = 0;
    let pointerY = 0;

    function clearPointer() {
      if (animationFrame) window.cancelAnimationFrame(animationFrame);
      animationFrame = 0;
      target.dataset.pointerActive = "false";
    }

    function handlePointerMove(event: PointerEvent) {
      if (event.pointerType !== "mouse" || !finePointer.matches || reducedMotion.matches) return;
      pointerX = event.clientX;
      pointerY = event.clientY;
      if (animationFrame) return;

      animationFrame = window.requestAnimationFrame(() => {
        target.style.setProperty("--rigx-pointer-x", `${pointerX}px`);
        target.style.setProperty("--rigx-pointer-y", `${pointerY}px`);
        target.dataset.pointerActive = "true";
        animationFrame = 0;
      });
    }

    function handlePointerOut(event: PointerEvent) {
      if (event.relatedTarget === null) clearPointer();
    }

    function handleVisibilityChange() {
      if (document.visibilityState === "hidden") clearPointer();
    }

    window.addEventListener("pointermove", handlePointerMove, { passive: true });
    window.addEventListener("pointerout", handlePointerOut, { passive: true });
    window.addEventListener("blur", clearPointer);
    document.addEventListener("visibilitychange", handleVisibilityChange);
    reducedMotion.addEventListener("change", clearPointer);

    return () => {
      if (animationFrame) window.cancelAnimationFrame(animationFrame);
      window.removeEventListener("pointermove", handlePointerMove);
      window.removeEventListener("pointerout", handlePointerOut);
      window.removeEventListener("blur", clearPointer);
      document.removeEventListener("visibilitychange", handleVisibilityChange);
      reducedMotion.removeEventListener("change", clearPointer);
    };
  }, [isAdmin]);

  if (isAdmin) return children;

  return (
    <>
      <div ref={visualEngineRef} className="rigx-visual-engine" aria-hidden="true" />
      <div className="rigx-app-frame">
        <div className="ambient ambient-one" aria-hidden="true" />
        <div className="ambient ambient-two" aria-hidden="true" />
        <div className="ambient ambient-three" aria-hidden="true" />
        <div className="pcb-overlay" aria-hidden="true" />
        <div className="edge-light edge-light-left" aria-hidden="true" />
        <div className="edge-light edge-light-right" aria-hidden="true" />
        <RigxHeader />
        <div id="main-content" className="shell main-content">{children}</div>
        <RigxFooter />
        <CartDrawer />
        <CartToast />
        <WhatsAppFloatingButton />
        <BackToTopButton />
      </div>
    </>
  );
}

