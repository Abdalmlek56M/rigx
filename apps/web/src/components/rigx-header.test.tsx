import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AppProviders } from "@/components/app-providers";
import { RigxHeader } from "@/components/rigx-header";
import * as apiClient from "@/lib/api-client";

vi.mock("next/navigation", () => ({
  usePathname: () => "/",
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), refresh: vi.fn() }),
}));

vi.mock("@/lib/api-client", async () => {
  const actual = await vi.importActual<typeof apiClient>("@/lib/api-client");
  return {
    ...actual,
    getCatalogCategories: vi.fn(),
    getCatalogProducts: vi.fn(),
    getCurrentUser: vi.fn(),
  };
});

const mockCategories: apiClient.CatalogCategory[] = [
  {
    id: "cat-1",
    parentId: null,
    name: "Processors",
    slug: "processors",
    description: null,
    iconStorageKey: null,
    sortOrder: 1,
    isActive: true,
    isEffectivelyActive: true,
    rowVersion: 1,
    children: [],
  },
  {
    id: "cat-2",
    parentId: null,
    name: "Graphics Cards",
    slug: "graphics-cards",
    description: null,
    iconStorageKey: null,
    sortOrder: 2,
    isActive: true,
    isEffectivelyActive: true,
    rowVersion: 1,
    children: [],
  },
];

const emptyProducts: apiClient.CatalogProductPage = {
  items: [],
  page: 1,
  pageSize: 5,
  totalCount: 0,
};

function renderHeader() {
  return render(
    <AppProviders>
      <RigxHeader />
    </AppProviders>,
  );
}

describe("RigxHeader navigation and departments menu", () => {
  beforeEach(() => {
    document.cookie = "hardware-store-locale=ar; Path=/";
    window.localStorage.setItem("hardware-store-locale", "ar");
    vi.mocked(apiClient.getCatalogCategories).mockResolvedValue(mockCategories);
    vi.mocked(apiClient.getCatalogProducts).mockResolvedValue(emptyProducts);
    vi.mocked(apiClient.getCurrentUser).mockResolvedValue(null);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    window.localStorage.clear();
    document.cookie = "hardware-store-locale=; Path=/; Max-Age=0";
    document.cookie = "hardware-store-theme=; Path=/; Max-Age=0";
  });

  it("renders the RIGX brand, primary navigation, search, and contact link", async () => {
    renderHeader();

    expect(screen.getByRole("link", { name: "RIGX Hardware" })).toHaveAttribute("href", "/");
    expect(screen.getAllByRole("link", { name: "المنتجات" }).length).toBeGreaterThan(0);
    expect(screen.getAllByRole("link", { name: "التجميعات" }).length).toBeGreaterThan(0);
    expect(screen.getAllByRole("link", { name: "المجلة" }).length).toBeGreaterThan(0);
    expect(screen.getByRole("link", { name: /تواصل معنا/ })).toHaveAttribute("href", "/contact");
    expect(screen.getByPlaceholderText(/ابحث عن منتج أو SKU/i)).toBeInTheDocument();
  });

  it("opens the departments menu from live catalog categories", async () => {
    renderHeader();

    await waitFor(() => expect(apiClient.getCatalogCategories).toHaveBeenCalled());
    await userEvent.click(screen.getByRole("button", { name: /كل الأقسام/ }));

    expect(screen.getByText("كل أقسام RIGX")).toBeInTheDocument();
    expect(screen.getAllByRole("link", { name: /المعالجات/ }).length).toBeGreaterThan(0);
    expect(screen.getAllByRole("link", { name: /كروت الشاشة/ }).length).toBeGreaterThan(0);
  });
});
