import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@/test/test-utils";
import userEvent from "@testing-library/user-event";
import { RegisterPage } from "./RegisterPage";
import * as authApi from "../api/auth";
import { ApiError } from "../api/client";

vi.mock("../api/auth");

describe("RegisterPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const getPasswordInput = (label: RegExp) =>
    screen.getByLabelText(label, { selector: "input" });

  it("renders registration form", () => {
    render(<RegisterPage />);

    expect(
      screen.getByRole("heading", { name: /create your account/i })
    ).toBeInTheDocument();
    expect(screen.getByLabelText(/^email$/i)).toBeInTheDocument();
    expect(getPasswordInput(/^password$/i)).toBeInTheDocument();
    expect(getPasswordInput(/confirm password/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /create account/i })
    ).toBeInTheDocument();
  });

  it("renders link to login page", () => {
    render(<RegisterPage />);

    const loginLink = screen.getByRole("link", { name: /sign in/i });
    expect(loginLink).toBeInTheDocument();
    expect(loginLink).toHaveAttribute("href", "/login");
  });

  it("handles successful registration", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.register).mockResolvedValueOnce({
      message:
        "Registration successful. Check your inbox to confirm your email.",
    });

    render(<RegisterPage />);

    await user.type(screen.getByLabelText(/^email$/i), "test@example.com");
    await user.type(getPasswordInput(/^password$/i), "password123");
    await user.type(getPasswordInput(/confirm password/i), "password123");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByText(/registration successful/i)).toBeInTheDocument();
    });
  });

  it("shows error when passwords do not match", async () => {
    const user = userEvent.setup();

    render(<RegisterPage />);

    await user.type(screen.getByLabelText(/^email$/i), "test@example.com");
    await user.type(getPasswordInput(/^password$/i), "password123");
    await user.type(
      getPasswordInput(/confirm password/i),
      "differentpassword"
    );
    await user.click(screen.getByRole("button", { name: /create account/i }));

    expect(screen.getByText(/passwords do not match/i)).toBeInTheDocument();
    expect(authApi.register).not.toHaveBeenCalled();
  });

  it("shows error when password is too short", async () => {
    const user = userEvent.setup();

    render(<RegisterPage />);

    await user.type(screen.getByLabelText(/^email$/i), "test@example.com");
    await user.type(getPasswordInput(/^password$/i), "short");
    await user.type(getPasswordInput(/confirm password/i), "short");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    expect(
      screen.getByText(/password must be at least 8 characters/i)
    ).toBeInTheDocument();
    expect(authApi.register).not.toHaveBeenCalled();
  });

  it("shows loading state during registration", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.register).mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    );

    render(<RegisterPage />);

    await user.type(screen.getByLabelText(/^email$/i), "test@example.com");
    await user.type(getPasswordInput(/^password$/i), "password123");
    await user.type(getPasswordInput(/confirm password/i), "password123");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it("shows error for duplicate email", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.register).mockRejectedValueOnce(
      new ApiError("Email already exists", 400, ["Email already in use"])
    );

    render(<RegisterPage />);

    await user.type(screen.getByLabelText(/^email$/i), "existing@example.com");
    await user.type(getPasswordInput(/^password$/i), "password123");
    await user.type(getPasswordInput(/confirm password/i), "password123");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByText(/email already in use/i)).toBeInTheDocument();
    });
  });

  it("shows API error message", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.register).mockRejectedValueOnce(
      new ApiError("Server error", 500)
    );

    render(<RegisterPage />);

    await user.type(screen.getByLabelText(/^email$/i), "test@example.com");
    await user.type(getPasswordInput(/^password$/i), "password123");
    await user.type(getPasswordInput(/confirm password/i), "password123");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByText(/server error/i)).toBeInTheDocument();
    });
  });

  it("shows generic error for unexpected errors", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.register).mockRejectedValueOnce(
      new Error("Network error")
    );

    render(<RegisterPage />);

    await user.type(screen.getByLabelText(/^email$/i), "test@example.com");
    await user.type(getPasswordInput(/^password$/i), "password123");
    await user.type(getPasswordInput(/confirm password/i), "password123");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/unexpected error while creating your account/i)
      ).toBeInTheDocument();
    });
  });

  it("clears password fields after successful registration", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.register).mockResolvedValueOnce({
      message: "Registration successful",
    });

    render(<RegisterPage />);

    const passwordInput = getPasswordInput(/^password$/i);
    const confirmInput = getPasswordInput(/confirm password/i);

    await user.type(screen.getByLabelText(/^email$/i), "test@example.com");
    await user.type(passwordInput, "password123");
    await user.type(confirmInput, "password123");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(passwordInput).toHaveValue("");
      expect(confirmInput).toHaveValue("");
    });
  });

  it("requires all fields", () => {
    render(<RegisterPage />);

    expect(screen.getByLabelText(/^email$/i)).toBeRequired();
    expect(getPasswordInput(/^password$/i)).toBeRequired();
    expect(getPasswordInput(/confirm password/i)).toBeRequired();
  });

  it("renders terms of service text", () => {
    render(<RegisterPage />);

    expect(
      screen.getByText(/by creating an account you agree/i)
    ).toBeInTheDocument();
  });

  it("handles API error with non-array data", async () => {
    const user = userEvent.setup();
    // Create an ApiError where data is not an array and no message
    const error = new ApiError("", 400);
    Object.defineProperty(error, "message", {
      value: "",
      writable: true,
    });
    Object.defineProperty(error, "data", {
      value: "Single error message",
      writable: true,
    });

    vi.mocked(authApi.register).mockRejectedValueOnce(error);

    render(<RegisterPage />);

    await user.type(screen.getByLabelText(/^email$/i), "test@example.com");
    await user.type(getPasswordInput(/^password$/i), "password123");
    await user.type(getPasswordInput(/confirm password/i), "password123");
    await user.click(screen.getByRole("button", { name: /create account/i }));

    await waitFor(() => {
      expect(screen.getByText(/unable to create account/i)).toBeInTheDocument();
    });
  });
});
