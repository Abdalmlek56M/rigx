import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useFragmentToken } from "./use-fragment-token";

function TokenProbe() {
  const { token, ready } = useFragmentToken();
  return <span>{ready ? (token ?? "missing") : "loading"}</span>;
}

describe("useFragmentToken", () => {
  it("reads the secret from the fragment and removes it from browser history", async () => {
    window.history.replaceState(null, "", "/verify-email#token=secret-token");
    render(<TokenProbe />);

    expect(await screen.findByText("secret-token")).toBeInTheDocument();
    expect(window.location.hash).toBe("");
    expect(window.location.search).toBe("");
  });
});
