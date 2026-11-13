"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { type FormEvent, useEffect, useMemo, useRef, useState } from "react";
import { getCatalogCategories, getCatalogProducts, type CatalogCategory, type CatalogProductSummary } from "@/lib/api-client";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { useTheme } from "@/components/theme-provider";
import { useCart } from "@/components/cart-provider";
import { Icon } from "@/components/icons";
import { localizedCategoryName } from "@/lib/category-labels";
import { formatCatalogPrice } from "@/lib/storefront";

type NavItem = { href: string; labelAr: string; labelEn: string; match: string };

const navItems: NavItem[] = [
  { href: "/", labelAr: "الرئيسية", labelEn: "Home", match: "/" },
  { href: "/products", labelAr: "المنتجات", labelEn: "Products", match: "/products" },
  { href: "/builds", labelAr: "التجميعات", labelEn: "Builds", match: "/builds" },
  { href: "/pc-builder", labelAr: "ابنِ جهازك", labelEn: "PC Builder", match: "/pc-builder" },
  { href: "/articles", labelAr: "المجلة", labelEn: "Journal", match: "/articles" },
];

const departmentFallback = [
  { slug: "graphics-cards", ar: "كروت الشاشة", en: "Graphics Cards", tag: "GPU" },
  { slug: "processors", ar: "المعالجات", en: "Processors", tag: "CPU" },
  { slug: "motherboards", ar: "اللوحات الأم", en: "Motherboards", tag: "MOTHERBOARD" },
  { slug: "memory", ar: "الذاكرة", en: "Memory", tag: "RAM" },
  { slug: "storage", ar: "وحدات التخزين", en: "Storage", tag: "SSD / HDD" },
  { slug: "power-supplies", ar: "مزودات الطاقة", en: "Power Supplies", tag: "PSU" },
  { slug: "cooling", ar: "التبريد", en: "Cooling", tag: "COOLING" },
  { slug: "cases", ar: "الصناديق", en: "Cases", tag: "CASES" },
];

type Suggestion = { id: string; name: string; slug: string; sku?: string; categoryName?: string; brandName?: string };

export function RigxHeader() {
  const { locale, toggleLocale } = useLocale();
  const { status, user, markSignedOut } = useSession();
  const { count: cartCount, openCart } = useCart();
  const { theme, toggleTheme } = useTheme();
  const pathname = usePathname();
  const router = useRouter();
  const isArabic = locale === "ar";

  const [megaOpen, setMegaOpen] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [term, setTerm] = useState("");
  const [suggestions, setSuggestions] = useState<Suggestion[]>([]);
  const [categories, setCategories] = useState<CatalogCategory[]>([]);
  const searchWrapRef = useRef<HTMLDivElement>(null);
  const searchInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    void getCatalogCategories()
      .then((items) => setCategories(items.filter((item) => item.isActive && item.isEffectivelyActive)))
      .catch(() => setCategories([]));
  }, []);

  useEffect(() => {
    setMegaOpen(false);
    setMobileOpen(false);
    setSearchOpen(false);
  }, [pathname]);

  useEffect(() => {
    function handleOutside(event: MouseEvent) {
      if (!searchWrapRef.current?.contains(event.target as Node)) setSearchOpen(false);
    }
    function handleKey(event: KeyboardEvent) {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        searchInputRef.current?.focus();
        setSearchOpen(true);
      }
      if (event.key === "Escape") {
        setSearchOpen(false);
        setMegaOpen(false);
      }
    }
    document.addEventListener("mousedown", handleOutside);
    document.addEventListener("keydown", handleKey);
    return () => {
      document.removeEventListener("mousedown", handleOutside);
      document.removeEventListener("keydown", handleKey);
    };
  }, []);

  useEffect(() => {
    const value = term.trim();
    if (value.length < 2) {
      setSuggestions([]);
      return;
    }
    let cancelled = false;
    const handle = window.setTimeout(() => {
      void getCatalogProducts({ search: value, pageSize: 5 })
        .then((page) => {
          if (cancelled) return;
          setSuggestions(page.items.map((item: CatalogProductSummary) => ({
            id: item.id, name: item.name, slug: item.slug, sku: item.sku,
            categoryName: item.categoryName, brandName: item.brandName ?? undefined,
          })));
        })
        .catch(() => setSuggestions([]));
    }, 220);
    return () => {
      cancelled = true;
      window.clearTimeout(handle);
    };
  }, [term]);

  function submitSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const value = term.trim();
    if (!value) return;
    setSearchOpen(false);
    router.push(`/products?q=${encodeURIComponent(value)}`);
  }

  const departments = useMemo<Array<{ slug: string; ar: string; en: string; tag: string; children: CatalogCategory[] }>>(() => {
    if (categories.length === 0) return departmentFallback.map((item) => ({ ...item, children: [] }));
    return categories.slice(0, 10).map((category) => ({
      slug: category.slug,
      ar: localizedCategoryName(category.slug, category.name, "ar"),
      en: localizedCategoryName(category.slug, category.name, "en"),
      tag: category.slug.replaceAll("-", " ").toUpperCase(),
      children: (category.children ?? []).filter((child) => child.isActive && child.isEffectivelyActive).slice(0, 4),
    }));
  }, [categories]);

  const active = (match: string) => (match === "/" ? pathname === "/" : pathname.startsWith(match));

  return (
    <header className="site-header" id="siteHeader">
      <div className="header-inner shell">
        <button className="mobile-menu icon-btn" type="button" aria-label={isArabic ? "القائمة" : "Menu"} onClick={() => setMobileOpen((open) => !open)}>
          <Icon name="menu" />
        </button>
        <Link className="brand" href="/" aria-label="RIGX Hardware">
          <span className="brand-mark">RIG<span>X</span></span>
          <small>HARDWARE</small>
        </Link>
        <nav className="main-nav" aria-label={isArabic ? "التنقل الرئيسي" : "Main navigation"}>
          {navItems.map((item) => (
            <Link key={item.href} href={item.href} className={`nav-link${active(item.match) ? " active" : ""}`}>
              {isArabic ? item.labelAr : item.labelEn}
            </Link>
          ))}
          <button className="nav-link" type="button" aria-expanded={megaOpen} onClick={() => setMegaOpen((open) => !open)}>
            {isArabic ? "كل الأقسام" : "Departments"} <span>⌄</span>
          </button>
        </nav>
        <div className="header-actions">
          <div className="search-wrap" ref={searchWrapRef} data-open={searchOpen ? "true" : "false"}>
            <svg className="search-icon" viewBox="0 0 24 24" aria-hidden="true"><circle cx="10.8" cy="10.8" r="6.8" /><path d="m16 16 5 5" /></svg>
            <form onSubmit={submitSearch} role="search">
              <input
                ref={searchInputRef}
                type="search"
                value={term}
                onChange={(event) => { setTerm(event.target.value); setSearchOpen(true); }}
                onFocus={() => setSearchOpen(true)}
                placeholder={isArabic ? "ابحث عن منتج أو SKU..." : "Search products or SKU..."}
                aria-label={isArabic ? "بحث" : "Search"}
                autoComplete="off"
              />
            </form>
            <kbd>Ctrl K</kbd>
            {searchOpen && term.trim().length >= 2 ? (
              <div className="search-results" role="listbox">
                {suggestions.length === 0 ? (
                  <p style={{ padding: "10px 12px", fontSize: 10, color: "var(--rigx-secondary)" }}>
                    {isArabic ? "لا نتائج مطابقة." : "No matching results."}
                  </p>
                ) : suggestions.map((item) => (
                  <Link key={item.id} href={`/products/${item.slug}`} className="search-result" role="option" aria-selected={false}>
                    <span>
                      <b>{item.name}</b>
                      <small>{item.brandName ? `${item.brandName} · ` : ""}{item.sku}</small>
                    </span>
                  </Link>
                ))}
                <button type="button" className="search-result" onClick={() => router.push(`/products?q=${encodeURIComponent(term.trim())}`)}>
                  <span><b>{isArabic ? "عرض كل النتائج" : "View all results"}</b><small>{term.trim()}</small></span>
                </button>
              </div>
            ) : null}
          </div>
          <Link className="header-contact-btn" href="/contact">
            <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 13a9 9 0 0 1 18 0" /><rect x="3" y="12" width="4" height="7" rx="2" /><rect x="17" y="12" width="4" height="7" rx="2" /><path d="M7 20h5" /></svg>
            <span>{isArabic ? "تواصل معنا" : "Contact"}</span>
          </Link>
          <button className="icon-btn" type="button" onClick={toggleTheme} aria-label={theme === "dark" ? (isArabic ? "التبديل إلى الوضع النهاري" : "Switch to light mode") : (isArabic ? "التبديل إلى الوضع الليلي" : "Switch to dark mode")}>
            <Icon name={theme === "dark" ? "sun" : "moon"} />
          </button>
          {status === "authenticated" && user ? (
            <Link className="icon-btn" href="/account" aria-label={isArabic ? "حسابي" : "My account"}>
              <Icon name="user" />
            </Link>
          ) : (
            <Link className="icon-btn" href="/login" aria-label={isArabic ? "تسجيل الدخول" : "Sign in"}>
              <Icon name="user" />
            </Link>
          )}
          <Link className="icon-btn" href="/account/wishlist" aria-label={isArabic ? "المفضلة" : "Wishlist"}>
            <Icon name="heart" />
          </Link>
          <button className="icon-btn cart-shortcut" type="button" onClick={openCart} aria-label={isArabic ? "عرض سلة التسوق" : "Open cart"}>
            <Icon name="shoppingBag" />
            <i className="mini-count">{cartCount}</i>
          </button>
          <button className="lang-btn" type="button" onClick={toggleLocale} aria-label={isArabic ? "Switch to English" : "التبديل إلى العربية"}>
            {isArabic ? "EN" : "AR"}
          </button>
          {status === "authenticated" ? (
            <button className="icon-btn" type="button" aria-label={isArabic ? "تسجيل الخروج" : "Sign out"} onClick={() => { void markSignedOut(); router.push("/"); }}>
              <Icon name="logout" />
            </button>
          ) : null}
        </div>
      </div>

      {mobileOpen ? (
        <nav className="mobile-nav open" aria-label={isArabic ? "قائمة الجوال" : "Mobile navigation"}>
          {navItems.map((item) => (
            <Link key={item.href} href={item.href}>{isArabic ? item.labelAr : item.labelEn}</Link>
          ))}
          <Link href="/categories">{isArabic ? "كل الأقسام" : "Departments"}</Link>
          <Link href="/account">{isArabic ? "حسابي" : "Account"}</Link>
          <Link href="/contact">{isArabic ? "تواصل معنا" : "Contact"}</Link>
        </nav>
      ) : null}

      {megaOpen ? (
        <div className="mega-menu shell" aria-label={isArabic ? "كل الأقسام" : "Departments"}>
          <div className="mega-menu-head">
            <div><span className="eyebrow">SHOP BY CATEGORY</span><b>{isArabic ? "كل أقسام RIGX" : "Every RIGX department"}</b></div>
            <button className="mega-close" type="button" onClick={() => setMegaOpen(false)} aria-label={isArabic ? "إغلاق" : "Close"}>×</button>
          </div>
          <div className="mega-menu-grid mega-department-tree">
            {departments.map((department) => (
              <section className="mega-department" key={department.slug}>
                <Link className="mega-parent" href={`/products?category=${encodeURIComponent(department.slug)}`}>
                  <span className="mega-parent-copy">
                    <b>{isArabic ? department.ar : department.en}</b>
                    <small>{department.tag}</small>
                  </span>
                  <span className="mega-parent-arrow">‹</span>
                </Link>
                <div className="mega-children">
                  {"children" in department && department.children && department.children.length > 0
                    ? department.children.map((child) => (
                      <Link key={child.id} href={`/products?category=${encodeURIComponent(child.slug)}`}>
                        {localizedCategoryName(child.slug, child.name, locale)}
                      </Link>
                    ))
                    : (
                      <Link href={`/products?category=${encodeURIComponent(department.slug)}`}>
                        {isArabic ? "عرض القسم" : "Browse department"}
                      </Link>
                    )}
                </div>
              </section>
            ))}
          </div>
          <div className="mega-menu-foot">
            {isArabic ? "كل الأقسام مرتبطة بالمخزون والكتالوج الفعلي في RIGX." : "Every department is wired to the live RIGX catalog and inventory."}
          </div>
        </div>
      ) : null}
    </header>
  );
}
