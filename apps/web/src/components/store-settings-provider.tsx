"use client";

import { createContext, type ReactNode, useContext, useEffect, useMemo, useState } from "react";
import { getStorefrontSettings, type StoreSettings } from "@/lib/api-client";

type StoreSettingsContextValue = {
  settings: StoreSettings | null;
  loading: boolean;
};

const StoreSettingsContext = createContext<StoreSettingsContextValue>({ settings: null, loading: true });

export function StoreSettingsProvider({ children }: { children: ReactNode }) {
  const [settings, setSettings] = useState<StoreSettings | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    getStorefrontSettings()
      .then((next) => {
        if (active) setSettings(next);
      })
      .catch(() => {
        if (active) setSettings(null);
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);

  const value = useMemo(() => ({ settings, loading }), [loading, settings]);
  return <StoreSettingsContext.Provider value={value}>{children}</StoreSettingsContext.Provider>;
}

export function useStoreSettings() {
  return useContext(StoreSettingsContext);
}
