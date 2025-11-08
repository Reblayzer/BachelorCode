import { describe, it, expect } from "vitest";
import { render, screen } from "@/test/test-utils";
import { EmailConfirmedPage } from "./EmailConfirmedPage";

describe("EmailConfirmedPage", () => {
  it("renders confirmation message", () => {
    render(<EmailConfirmedPage />);

    expect(
      screen.getByRole("heading", { name: /email confirmed/i })
    ).toBeInTheDocument();
    expect(
      screen.getByText(/your email address has been successfully verified/i)
    ).toBeInTheDocument();
  });

  it("renders success icon", () => {
    const { container } = render(<EmailConfirmedPage />);

    const svg = container.querySelector("svg");
    expect(svg).toBeInTheDocument();
    expect(svg).toHaveClass("text-emerald-600");
  });

  it("renders continue to login button", () => {
    render(<EmailConfirmedPage />);

    const button = screen.getByRole("button", { name: /continue to login/i });
    expect(button).toBeInTheDocument();
  });

  it("has link to login page", () => {
    render(<EmailConfirmedPage />);

    const link = screen.getByRole("link");
    expect(link).toHaveAttribute("href", "/login");
  });

  it("renders with centered layout", () => {
    const { container } = render(<EmailConfirmedPage />);

    const card = container.querySelector(".p-8.text-center");
    expect(card).toBeInTheDocument();
  });
});
