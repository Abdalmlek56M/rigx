export const THEME_STORAGE_KEY = "rigx.theme";

/** Runs in <head> before first paint: the saved choice, else light (the default look since 4.10). */
export const THEME_INIT_SCRIPT = `try{var t=localStorage.getItem("${THEME_STORAGE_KEY}");if(t!=="dark")t="light";document.documentElement.dataset.theme=t}catch(e){}`;
