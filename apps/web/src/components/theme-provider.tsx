"use client";

import { createContext, type ReactNode, useContext, useEffect, useMemo, useState } from "react";

export type Theme = "light" | "dark";

type ThemeContextValue = {
  theme: Theme;
  toggleTheme: () => void;
};

const storageKey = "hardware-store-theme";
const cookieName = "hardware-store-theme";
const ThemeContext = createContext<ThemeContextValue | null>(null);

function readTheme(): Theme {
  return document.documentElement.dataset.theme === "dark" ? "dark" : "light";
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<Theme>("dark");

  useEffect(() => {
    const current = readTheme();
    setTheme(current);
    document.documentElement.style.colorScheme = current;
  }, []);

  const value = useMemo<ThemeContextValue>(() => ({
    theme,
    toggleTheme: () => {
      const next = readTheme() === "dark" ? "light" : "dark";
      document.documentElement.dataset.theme = next;
      document.documentElement.style.colorScheme = next;
      window.localStorage.setItem(storageKey, next);
      document.cookie = `${cookieName}=${next}; Path=/; Max-Age=31536000; SameSite=Lax`;
      setTheme(next);
    },
  }), [theme]);

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme(): ThemeContextValue {
  const value = useContext(ThemeContext);
  if (!value) throw new Error("useTheme must be used inside ThemeProvider.");
  return value;
}
