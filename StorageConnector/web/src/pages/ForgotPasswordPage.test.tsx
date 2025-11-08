import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@/test/test-utils";
import userEvent from "@testing-library/user-event";
import { ForgotPasswordPage } from "./ForgotPasswordPage";
import * as authApi from "../api/auth";
import { ApiError } from "../api/client";

vi.mock("../api/auth");

describe("ForgotPasswordPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders forgot password form", () => {
    render(<ForgotPasswordPage />);

    expect(
      screen.getByRole("heading", { name: /forgot password/i })
    ).toBeInTheDocument();
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /send reset link/i })
    ).toBeInTheDocument();
  });

  it("renders description text", () => {
    render(<ForgotPasswordPage />);

    expect(
      screen.getByText(/enter your email address and we'll send you a link/i)
    ).toBeInTheDocument();
  });

  it("renders back to login link", () => {
    render(<ForgotPasswordPage />);

    const loginLink = screen.getByRole("link", { name: /back to login/i });
    expect(loginLink).toBeInTheDocument();
    expect(loginLink).toHaveAttribute("href", "/login");
  });

  it("handles successful password reset request", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.requestPasswordReset).mockResolvedValueOnce(undefined);

    render(<ForgotPasswordPage />);

    const emailInput = screen.getByLabelText(/email address/i);
    await user.type(emailInput, "test@example.com");
    await user.click(screen.getByRole("button", { name: /send reset link/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/password reset link has been sent/i)
      ).toBeInTheDocument();
    });
  });

  it("clears email field after successful submission", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.requestPasswordReset).mockResolvedValueOnce(undefined);

    render(<ForgotPasswordPage />);

    const emailInput = screen.getByLabelText(/email address/i);
    await user.type(emailInput, "test@example.com");
    await user.click(screen.getByRole("button", { name: /send reset link/i }));

    await waitFor(() => {
      expect(emailInput).toHaveValue("");
    });
  });

  it("shows loading state during submission", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.requestPasswordReset).mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    );

    render(<ForgotPasswordPage />);

    await user.type(
      screen.getByLabelText(/email address/i),
      "test@example.com"
    );
    await user.click(screen.getByRole("button", { name: /send reset link/i }));

    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it("shows error message on API error", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.requestPasswordReset).mockRejectedValueOnce(
      new ApiError("User not found", 404)
    );

    render(<ForgotPasswordPage />);

    await user.type(
      screen.getByLabelText(/email address/i),
      "unknown@example.com"
    );
    await user.click(screen.getByRole("button", { name: /send reset link/i }));

    await waitFor(() => {
      expect(screen.getByText(/user not found/i)).toBeInTheDocument();
    });
  });

  it("shows generic error for unexpected errors", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.requestPasswordReset).mockRejectedValueOnce(
      new Error("Network error")
    );

    render(<ForgotPasswordPage />);

    await user.type(
      screen.getByLabelText(/email address/i),
      "test@example.com"
    );
    await user.click(screen.getByRole("button", { name: /send reset link/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/unexpected error occurred/i)
      ).toBeInTheDocument();
    });
  });

  it("requires email field", () => {
    render(<ForgotPasswordPage />);

    expect(screen.getByLabelText(/email address/i)).toBeRequired();
  });

  it("has email input type", () => {
    render(<ForgotPasswordPage />);

    expect(screen.getByLabelText(/email address/i)).toHaveAttribute(
      "type",
      "email"
    );
  });
});
