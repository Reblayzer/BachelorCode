import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@/test/test-utils";
import userEvent from "@testing-library/user-event";
import { ResetPasswordPage } from "./ResetPasswordPage";
import * as authApi from "../api/auth";
import { ApiError } from "../api/client";

vi.mock("../api/auth");

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useSearchParams: () => [
      new URLSearchParams("?token=valid-token&email=test@example.com"),
    ],
  };
});

describe("ResetPasswordPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders reset password form with valid token", () => {
    render(<ResetPasswordPage />);

    expect(
      screen.getByRole("heading", { name: /reset password/i })
    ).toBeInTheDocument();
    expect(
      screen.getByText(/enter your new password below/i)
    ).toBeInTheDocument();
    expect(screen.getByLabelText(/^new password$/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/confirm password/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /reset password/i })
    ).toBeInTheDocument();
  });

  it("handles successful password reset", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.resetPassword).mockResolvedValueOnce(undefined);

    render(<ResetPasswordPage />);

    await user.type(screen.getByLabelText(/^new password$/i), "newpassword123");
    await user.type(
      screen.getByLabelText(/confirm password/i),
      "newpassword123"
    );
    await user.click(screen.getByRole("button", { name: /reset password/i }));

    await waitFor(() => {
      expect(authApi.resetPassword).toHaveBeenCalled();
      expect(mockNavigate).toHaveBeenCalledWith("/login", {
        state: {
          message: expect.stringContaining("Password reset successfully"),
        },
      });
    });
  });

  it("shows error when passwords do not match", async () => {
    const user = userEvent.setup();

    render(<ResetPasswordPage />);

    await user.type(screen.getByLabelText(/^new password$/i), "password123");
    await user.type(
      screen.getByLabelText(/confirm password/i),
      "differentpassword"
    );
    await user.click(screen.getByRole("button", { name: /reset password/i }));

    expect(screen.getByText(/passwords do not match/i)).toBeInTheDocument();
    expect(authApi.resetPassword).not.toHaveBeenCalled();
  });

  it("shows error when password is too short", async () => {
    const user = userEvent.setup();

    render(<ResetPasswordPage />);

    await user.type(screen.getByLabelText(/^new password$/i), "short");
    await user.type(screen.getByLabelText(/confirm password/i), "short");
    await user.click(screen.getByRole("button", { name: /reset password/i }));

    expect(
      screen.getByText(/password must be at least 8 characters/i)
    ).toBeInTheDocument();
    expect(authApi.resetPassword).not.toHaveBeenCalled();
  });

  it("shows loading state during password reset", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.resetPassword).mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    );

    render(<ResetPasswordPage />);

    await user.type(screen.getByLabelText(/^new password$/i), "newpassword123");
    await user.type(
      screen.getByLabelText(/confirm password/i),
      "newpassword123"
    );
    await user.click(screen.getByRole("button", { name: /reset password/i }));

    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it("shows API error message", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.resetPassword).mockRejectedValueOnce(
      new ApiError("Invalid or expired token", 400)
    );

    render(<ResetPasswordPage />);

    await user.type(screen.getByLabelText(/^new password$/i), "newpassword123");
    await user.type(
      screen.getByLabelText(/confirm password/i),
      "newpassword123"
    );
    await user.click(screen.getByRole("button", { name: /reset password/i }));

    await waitFor(() => {
      expect(screen.getByText(/invalid or expired token/i)).toBeInTheDocument();
    });
  });

  it("shows generic error for unexpected errors", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.resetPassword).mockRejectedValueOnce(
      new Error("Network error")
    );

    render(<ResetPasswordPage />);

    await user.type(screen.getByLabelText(/^new password$/i), "newpassword123");
    await user.type(
      screen.getByLabelText(/confirm password/i),
      "newpassword123"
    );
    await user.click(screen.getByRole("button", { name: /reset password/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/unexpected error occurred/i)
      ).toBeInTheDocument();
    });
  });

  it("requires password fields", () => {
    render(<ResetPasswordPage />);

    expect(screen.getByLabelText(/^new password$/i)).toBeRequired();
    expect(screen.getByLabelText(/^confirm password$/i)).toBeRequired();
  });
});
