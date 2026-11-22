export const THEME_STORAGE_KEY = "rigx.theme";
export const THEME_COOKIE = "rigx.theme";

/** A year: the choice is a preference, not a session. */
export const THEME_COOKIE_MAX_AGE = 60 * 60 * 24 * 365;

export type Theme = "light" | "dark";

/** Anything that is not "dark" is the default look (light since 4.10). */
export function parseTheme(value: string | null | undefined): Theme {
  return value === "dark" ? "dark" : "light";
}

/** The cookie text that remembers a theme for the server (it writes `data-theme` into the page, so a language switch never repaints light). */
export function themeCookie(theme: Theme): string {
  return `${THEME_COOKIE}=${theme}; path=/; max-age=${THEME_COOKIE_MAX_AGE}; samesite=lax`;
}

/**
 * Runs in <head> before first paint, as the fallback for the server's `data-theme`: the cookie, else the choice saved before the cookie
 * existed (copied into the cookie once), else light. It always sets the attribute, so a page the server could not decorate still paints right.
 */
export const THEME_INIT_SCRIPT = `try{var d=document.documentElement,m=document.cookie.match(/(?:^|; )${THEME_COOKIE.replaceAll(".", "\\.")}=(light|dark)/),t=m?m[1]:localStorage.getItem("${THEME_STORAGE_KEY}");if(t!=="dark")t="light";d.dataset.theme=t;if(!m&&t==="dark")document.cookie="${themeCookie("dark")}"}catch(e){}`;
