import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@/test/test-utils";
import userEvent from "@testing-library/user-event";
import { AccountPage } from "./AccountPage";
import * as authApi from "../api/auth";
import { ApiError } from "../api/client";
import { useAuthStore } from "../state/auth-store";

vi.mock("../api/auth");

// Mock the auth store
vi.mock("../state/auth-store", () => ({
  useAuthStore: vi.fn(),
}));

describe("AccountPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useAuthStore).mockReturnValue("test@example.com");
  });

  it("renders account settings page", () => {
    render(<AccountPage />);

    expect(
      screen.getByRole("heading", { name: /account settings/i })
    ).toBeInTheDocument();
    expect(
      screen.getByText(/manage your account preferences/i)
    ).toBeInTheDocument();
  });

  it("displays user email", () => {
    render(<AccountPage />);

    expect(screen.getByText(/^email$/i)).toBeInTheDocument();
    expect(screen.getByText("test@example.com")).toBeInTheDocument();
  });

  it("renders change password form", () => {
    render(<AccountPage />);

    expect(
      screen.getByRole("heading", { name: /change password/i })
    ).toBeInTheDocument();
    expect(screen.getByLabelText(/current password/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^new password$/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/confirm new password/i)).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /change password/i })
    ).toBeInTheDocument();
  });

  it("handles successful password change", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.changePassword).mockResolvedValueOnce(undefined);

    render(<AccountPage />);

    await user.type(
      screen.getByLabelText(/current password/i),
      "currentpass123"
    );
    await user.type(screen.getByLabelText(/^new password$/i), "newpassword123");
    await user.type(
      screen.getByLabelText(/confirm new password/i),
      "newpassword123"
    );
    await user.click(screen.getByRole("button", { name: /change password/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/password changed successfully/i)
      ).toBeInTheDocument();
    });
  });

  it("clears password fields after successful change", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.changePassword).mockResolvedValueOnce(undefined);

    render(<AccountPage />);

    const currentInput = screen.getByLabelText(/current password/i);
    const newInput = screen.getByLabelText(/^new password$/i);
    const confirmInput = screen.getByLabelText(/confirm new password/i);

    await user.type(currentInput, "currentpass123");
    await user.type(newInput, "newpassword123");
    await user.type(confirmInput, "newpassword123");
    await user.click(screen.getByRole("button", { name: /change password/i }));

    await waitFor(() => {
      expect(currentInput).toHaveValue("");
      expect(newInput).toHaveValue("");
      expect(confirmInput).toHaveValue("");
    });
  });

  it("shows error when new passwords do not match", async () => {
    const user = userEvent.setup();

    render(<AccountPage />);

    await user.type(
      screen.getByLabelText(/current password/i),
      "currentpass123"
    );
    await user.type(screen.getByLabelText(/^new password$/i), "newpassword123");
    await user.type(
      screen.getByLabelText(/confirm new password/i),
      "differentpassword"
    );
    await user.click(screen.getByRole("button", { name: /change password/i }));

    expect(screen.getByText(/new passwords do not match/i)).toBeInTheDocument();
    expect(authApi.changePassword).not.toHaveBeenCalled();
  });

  it("shows error when new password is too short", async () => {
    const user = userEvent.setup();

    render(<AccountPage />);

    await user.type(
      screen.getByLabelText(/current password/i),
      "currentpass123"
    );
    await user.type(screen.getByLabelText(/^new password$/i), "short");
    await user.type(screen.getByLabelText(/confirm new password/i), "short");
    await user.click(screen.getByRole("button", { name: /change password/i }));

    expect(
      screen.getByText(/new password must be at least 8 characters/i)
    ).toBeInTheDocument();
    expect(authApi.changePassword).not.toHaveBeenCalled();
  });

  it("shows loading state during password change", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.changePassword).mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 100))
    );

    render(<AccountPage />);

    await user.type(
      screen.getByLabelText(/current password/i),
      "currentpass123"
    );
    await user.type(screen.getByLabelText(/^new password$/i), "newpassword123");
    await user.type(
      screen.getByLabelText(/confirm new password/i),
      "newpassword123"
    );
    await user.click(screen.getByRole("button", { name: /change password/i }));

    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it("shows API error message", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.changePassword).mockRejectedValueOnce(
      new ApiError("Current password is incorrect", 400)
    );

    render(<AccountPage />);

    await user.type(
      screen.getByLabelText(/current password/i),
      "wrongpassword"
    );
    await user.type(screen.getByLabelText(/^new password$/i), "newpassword123");
    await user.type(
      screen.getByLabelText(/confirm new password/i),
      "newpassword123"
    );
    await user.click(screen.getByRole("button", { name: /change password/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/current password is incorrect/i)
      ).toBeInTheDocument();
    });
  });

  it("shows generic error for unexpected errors", async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.changePassword).mockRejectedValueOnce(
      new Error("Network error")
    );

    render(<AccountPage />);

    await user.type(
      screen.getByLabelText(/current password/i),
      "currentpass123"
    );
    await user.type(screen.getByLabelText(/^new password$/i), "newpassword123");
    await user.type(
      screen.getByLabelText(/confirm new password/i),
      "newpassword123"
    );
    await user.click(screen.getByRole("button", { name: /change password/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/unexpected error occurred/i)
      ).toBeInTheDocument();
    });
  });

  it("requires all password fields", () => {
    render(<AccountPage />);

    expect(screen.getByLabelText(/current password/i)).toBeRequired();
    expect(screen.getByLabelText(/^new password$/i)).toBeRequired();
    expect(screen.getByLabelText(/confirm new password/i)).toBeRequired();
  });

  it("has password input types", () => {
    render(<AccountPage />);

    expect(screen.getByLabelText(/current password/i)).toHaveAttribute(
      "type",
      "password"
    );
    expect(screen.getByLabelText(/^new password$/i)).toHaveAttribute(
      "type",
      "password"
    );
    expect(screen.getByLabelText(/confirm new password/i)).toHaveAttribute(
      "type",
      "password"
    );
  });
});
