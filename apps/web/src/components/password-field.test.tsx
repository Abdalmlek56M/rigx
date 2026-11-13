import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AppProviders } from "@/components/app-providers";
import { PasswordField } from "@/components/ui";
import { useLocale } from "@/lib/i18n";

describe("PasswordField", () => {
  afterEach(() => {
    vi.restoreAllMocks();
    window.localStorage.clear();
    document.cookie = "hardware-store-locale=; Path=/; Max-Age=0";
    document.cookie = "hardware-store-locale=ar; Path=/";
    window.localStorage.setItem("hardware-store-locale", "ar");
  });

  it("toggles visibility with an accessible button while preserving the value and focus", async () => {
    render(
      <AppProviders initialLocale="ar">
        <PasswordField label="كلمة المرور" defaultValue="a sufficiently long password" />
      </AppProviders>,
    );

    const input = screen.getByLabelText("كلمة المرور");
    expect(input).toHaveAttribute("type", "password");

    const showButton = screen.getByRole("button", { name: "إظهار كلمة المرور" });
    expect(showButton).toHaveAttribute("type", "button");
    await userEvent.click(showButton);

    expect(input).toHaveAttribute("type", "text");
    expect(input).toHaveValue("a sufficiently long password");
    expect(input).toHaveFocus();
    expect(screen.getByRole("button", { name: "إخفاء كلمة المرور" })).toHaveAttribute("aria-pressed", "true");
  });

  it("updates the visibility labels when the language changes", async () => {
    render(
      <AppProviders initialLocale="ar">
        <LocaleSwitchHarness />
      </AppProviders>,
    );

    expect(screen.getByRole("button", { name: "إظهار كلمة المرور" })).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Switch language" }));

    expect(screen.getByRole("button", { name: "Show password" })).toBeInTheDocument();
    expect(document.documentElement).toHaveAttribute("lang", "en");
    expect(document.documentElement).toHaveAttribute("dir", "ltr");
  });
});

function LocaleSwitchHarness() {
  const { toggleLocale } = useLocale();
  return (
    <>
      <button type="button" onClick={toggleLocale}>Switch language</button>
      <PasswordField label="Password" defaultValue="value" />
    </>
  );
}
