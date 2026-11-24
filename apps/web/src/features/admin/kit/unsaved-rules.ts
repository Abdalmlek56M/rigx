/** Leaving a screen with unsaved edits: which clicks leave the page (pure, so it is tested). */

export interface ClickInfo {
  button: number;
  metaKey: boolean;
  ctrlKey: boolean;
  shiftKey: boolean;
  altKey: boolean;
  defaultPrevented: boolean;
}

export interface AnchorInfo {
  href: string;
  target: string | null;
  download: boolean;
}

export interface Here {
  origin: string;
  pathname: string;
  search: string;
}

/**
 * The path (with query and hash) a click would take the visitor to, or null when it does not leave the screen: a click that is not a plain
 * left click, a link that opens elsewhere (new tab, download, another site), or one that only changes the hash of this very page.
 */
export function leavingTarget(click: ClickInfo, anchor: AnchorInfo, here: Here): string | null {
  if (click.defaultPrevented || click.button !== 0 || click.metaKey || click.ctrlKey || click.shiftKey || click.altKey) return null;
  if (anchor.download || (anchor.target !== null && anchor.target !== "" && anchor.target !== "_self")) return null;
  let url: URL;
  try {
    url = new URL(anchor.href, here.origin);
  } catch {
    return null;
  }
  if (url.origin !== here.origin) return null;
  if (url.pathname === here.pathname && url.search === here.search) return null;
  return `${url.pathname}${url.search}${url.hash}`;
}
