import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@/test/test-utils";
import userEvent from "@testing-library/user-event";
import { LoginPage } from "./LoginPage";
import * as authApi from "../api/auth";
import { ApiError } from "../api/client";

// Mock the auth API
vi.mock("../api/auth");

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe("LoginPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders login form", () => {
    render(<LoginPage />);

    expect(
      screen.getByRole("heading", { name: /welcome back/i })
    ).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /sign in/i })
    ).toBeInTheDocument();
  });

  it("renders link to register page", () => {
    render(<LoginPage />);

    const registerLink = screen.getByRole("link", {
      name: /create an account/i,
    });
    expect(registerLink).toBeInTheDocument();
    expect(registerLink).toHaveAttribute("href", "/register");
  });

  it("renders forgot password link", () => {
    render(<LoginPage />);

    const forgotLink = screen.getByRole("link", { name: /forgot password/i });
    expect(forgotLink).toBeInTheDocument();
    expect(forgotLink).toHaveAttribute("href", "/forgot-password");
  });

  it("handles successful login", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.login).mockResolvedValueOnce({ token: "fake-jwt-token" });

    render(<LoginPage />);

    await user.type(screen.getByLabelText(/email/i), "test@example.com");
    await user.type(screen.getByLabelText(/password/i), "password123");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(authApi.login).toHaveBeenCalled();
      const callArgs = vi.mocked(authApi.login).mock.calls[0][0];
      expect(callArgs).toEqual({
        email: "test@example.com",
        password: "password123",
      });
    });

    await waitFor(() => {
      expect(mockNavigate).toHaveBeenCalledWith("/connections");
    });
  });

  it("shows loading state during login", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.login).mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    );

    render(<LoginPage />);

    await user.type(screen.getByLabelText(/email/i), "test@example.com");
    await user.type(screen.getByLabelText(/password/i), "password123");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    expect(screen.getByRole("button", { name: /signing in/i })).toBeDisabled();
  });

  it("shows error message for unconfirmed email (403)", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.login).mockRejectedValueOnce(
      new ApiError("Email not confirmed", 403)
    );

    render(<LoginPage />);

    await user.type(screen.getByLabelText(/email/i), "test@example.com");
    await user.type(screen.getByLabelText(/password/i), "password123");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/please confirm your email address/i)
      ).toBeInTheDocument();
    });
  });

  it("shows error message for incorrect credentials (401)", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.login).mockRejectedValueOnce(
      new ApiError("Invalid credentials", 401)
    );

    render(<LoginPage />);

    await user.type(screen.getByLabelText(/email/i), "test@example.com");
    await user.type(screen.getByLabelText(/password/i), "wrongpassword");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/email or password is incorrect/i)
      ).toBeInTheDocument();
    });
  });

  it("shows error message for API errors", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.login).mockRejectedValueOnce(
      new ApiError("Server error", 500)
    );

    render(<LoginPage />);

    await user.type(screen.getByLabelText(/email/i), "test@example.com");
    await user.type(screen.getByLabelText(/password/i), "password123");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText(/server error/i)).toBeInTheDocument();
    });
  });

  it("shows generic error for unexpected errors", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.login).mockRejectedValueOnce(new Error("Network error"));

    render(<LoginPage />);

    await user.type(screen.getByLabelText(/email/i), "test@example.com");
    await user.type(screen.getByLabelText(/password/i), "password123");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/unexpected error while signing in/i)
      ).toBeInTheDocument();
    });
  });

  it("requires email and password fields", () => {
    render(<LoginPage />);

    const emailInput = screen.getByLabelText(/email/i);
    const passwordInput = screen.getByLabelText(/password/i);

    expect(emailInput).toBeRequired();
    expect(passwordInput).toBeRequired();
  });

  it("has correct input types", () => {
    render(<LoginPage />);

    expect(screen.getByLabelText(/email/i)).toHaveAttribute("type", "email");
    expect(screen.getByLabelText(/password/i)).toHaveAttribute(
      "type",
      "password"
    );
  });

  it("has autocomplete attributes", () => {
    render(<LoginPage />);

    expect(screen.getByLabelText(/email/i)).toHaveAttribute(
      "autocomplete",
      "email"
    );
    expect(screen.getByLabelText(/password/i)).toHaveAttribute(
      "autocomplete",
      "current-password"
    );
  });

  it("renders helper text", () => {
    render(<LoginPage />);

    expect(screen.getByText(/trouble signing in/i)).toBeInTheDocument();
  });
});
