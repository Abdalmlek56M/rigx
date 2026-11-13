"use client";

import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { usePathname } from "next/navigation";
import { getPublishedHomepageLayout } from "@/lib/api-client";
import {
  createDefaultHomeLayout,
  parseHomeLayout,
  type HomeLayout,
} from "@/lib/home-layout";

type HomeLayoutContextValue = {
  layout: HomeLayout;
  ready: boolean;
  updateLayout: (updater: (current: HomeLayout) => HomeLayout) => void;
  resetLayout: () => void;
  loadLayout: (layout: HomeLayout) => void;
};

const HomeLayoutContext = createContext<HomeLayoutContextValue | null>(null);

/**
 * Homepage content is owned by the backend CMS. The storefront renders the
 * last published layout, and falls back to the built-in default layout when no
 * published configuration exists or the API is temporarily unavailable.
 */
export function HomeLayoutProvider({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const [layout, setLayout] = useState<HomeLayout>(() => createDefaultHomeLayout());
  const [ready, setReady] = useState(false);

  useEffect(() => {
    if (pathname !== "/") {
      setReady(true);
      return;
    }
    let active = true;
    void getPublishedHomepageLayout()
      .then((record) => {
        if (active) setLayout(parseHomeLayout(record.layoutJson));
      })
      .catch(() => {
        if (active) setLayout(createDefaultHomeLayout());
      })
      .finally(() => {
        if (active) setReady(true);
      });
    return () => {
      active = false;
    };
  }, [pathname]);

  const updateLayout = useCallback((updater: (current: HomeLayout) => HomeLayout) => {
    setLayout((current) => updater(current));
  }, []);

  const resetLayout = useCallback(() => {
    setLayout(createDefaultHomeLayout());
  }, []);

  const loadLayout = useCallback((next: HomeLayout) => {
    setLayout(next);
  }, []);

  const value = useMemo(
    () => ({ layout, ready, updateLayout, resetLayout, loadLayout }),
    [layout, ready, updateLayout, resetLayout, loadLayout],
  );

  return <HomeLayoutContext.Provider value={value}>{children}</HomeLayoutContext.Provider>;
}

export function useHomeLayout(): HomeLayoutContextValue {
  const context = useContext(HomeLayoutContext);
  if (!context) {
    throw new Error("useHomeLayout must be used inside HomeLayoutProvider");
  }
  return context;
}
