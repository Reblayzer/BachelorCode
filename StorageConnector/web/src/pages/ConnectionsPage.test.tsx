import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@/test/test-utils";
import { ConnectionsPage } from "./ConnectionsPage";
import { useAuthStore } from "../state/auth-store";
import { getConnections, startLink, disconnect } from "../api/connections";
import { ApiError } from "../api/client";
import userEvent from "@testing-library/user-event";

vi.mock("../api/connections");
vi.mock("../state/auth-store", () => ({
  useAuthStore: vi.fn(),
}));

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

describe("ConnectionsPage", () => {
  let mockSetAuthenticated: ReturnType<typeof vi.fn>;
  let mockClear: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.clearAllMocks();
    mockSetAuthenticated = vi.fn();
    mockClear = vi.fn();

    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: true,
        userEmail: "test@example.com",
        setAuthenticated: mockSetAuthenticated,
        clear: mockClear,
      };
      return selector(mockState);
    });

    vi.mocked(getConnections).mockResolvedValue([
      {
        provider: "Google",
        isLinked: true,
        scopes: ["https://www.googleapis.com/auth/drive.readonly"],
      },
      {
        provider: "Microsoft",
        isLinked: false,
        scopes: [],
      },
    ]);
  });

  it("renders connections page when authenticated", async () => {
    render(<ConnectionsPage />);

    await waitFor(() => {
      expect(screen.getByText("Your connections")).toBeInTheDocument();
    });

    expect(
      screen.getByText(/Link your Google and Microsoft accounts/i)
    ).toBeInTheDocument();
  });

  it("displays provider connection status", async () => {
    render(<ConnectionsPage />);

    await waitFor(() => {
      expect(screen.getByText("Google Drive")).toBeInTheDocument();
    });

    // Verify status badges exist (either "Linked" or "Not linked")
    const statusBadges = screen.getAllByText(/linked/i);
    expect(statusBadges.length).toBeGreaterThan(0);

    // Verify at least one provider shows scope information
    const googleCard = screen.getByText("Google Drive").closest("div");
    expect(googleCard).toBeInTheDocument();
  });

  it("displays disconnected providers", async () => {
    render(<ConnectionsPage />);

    await waitFor(() => {
      expect(screen.getByText("Microsoft OneDrive")).toBeInTheDocument();
    });

    const notLinkedBadges = screen.getAllByText("Not linked");
    expect(notLinkedBadges.length).toBeGreaterThan(0);
  });

  it("handles successful connection start", async () => {
    const user = userEvent.setup();
    const mockLocation = { href: "" };
    Object.defineProperty(window, "location", {
      value: mockLocation,
      writable: true,
    });

    vi.mocked(startLink).mockResolvedValue({
      redirectUrl: "https://accounts.google.com/oauth",
    });

    render(<ConnectionsPage />);

    await waitFor(() => {
      expect(screen.getByText("Microsoft OneDrive")).toBeInTheDocument();
    });

    const connectButtons = screen.getAllByRole("button", { name: /connect/i });
    await user.click(connectButtons[1]);

    await waitFor(() => {
      expect(screen.getByText("Redirecting to provider…")).toBeInTheDocument();
    });

    expect(mockLocation.href).toBe("https://accounts.google.com/oauth");
  });

  it("handles connection start error", async () => {
    const user = userEvent.setup();
    vi.mocked(startLink).mockRejectedValue(
      new ApiError("Connection failed", 500)
    );

    render(<ConnectionsPage />);

    await waitFor(() => {
      expect(screen.getByText("Microsoft OneDrive")).toBeInTheDocument();
    });

    const connectButtons = screen.getAllByRole("button", { name: /connect/i });
    await user.click(connectButtons[1]);

    await waitFor(() => {
      expect(
        screen.getByText(/Unable to start the Microsoft link/i)
      ).toBeInTheDocument();
    });
  });

  it("handles successful disconnection", async () => {
    const user = userEvent.setup();
    vi.mocked(disconnect).mockResolvedValue(undefined);

    // Clear previous mock and set new one with linked connection
    vi.mocked(getConnections).mockClear();
    vi.mocked(getConnections).mockResolvedValue([
      {
        provider: "Google",
        isLinked: true,
        scopes: ["https://www.googleapis.com/auth/drive.readonly"],
      },
      {
        provider: "Microsoft",
        isLinked: false,
        scopes: [],
      },
    ]);

    render(<ConnectionsPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: /disconnect/i })
      ).toBeInTheDocument();
    });

    const disconnectButton = screen.getByRole("button", {
      name: /disconnect/i,
    });
    await user.click(disconnectButton);

    await waitFor(() => {
      expect(screen.getByText(/Google disconnected/i)).toBeInTheDocument();
    });

    expect(disconnect).toHaveBeenCalledWith("Google", expect.any(Object));
  });

  it("handles disconnection error", async () => {
    const user = userEvent.setup();
    vi.mocked(disconnect).mockRejectedValue(
      new ApiError("Disconnect failed", 500)
    );

    // Clear previous mock and set new one with linked connection
    vi.mocked(getConnections).mockClear();
    vi.mocked(getConnections).mockResolvedValue([
      {
        provider: "Google",
        isLinked: true,
        scopes: ["https://www.googleapis.com/auth/drive.readonly"],
      },
      {
        provider: "Microsoft",
        isLinked: false,
        scopes: [],
      },
    ]);

    render(<ConnectionsPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: /disconnect/i })
      ).toBeInTheDocument();
    });

    const disconnectButton = screen.getByRole("button", {
      name: /disconnect/i,
    });
    await user.click(disconnectButton);

    await waitFor(() => {
      expect(
        screen.getByText(/Unable to disconnect Google/i)
      ).toBeInTheDocument();
    });
  });

  it("shows sign in prompt when not authenticated", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: false,
        userEmail: null,
        setAuthenticated: mockSetAuthenticated,
        clear: mockClear,
      };
      return selector(mockState);
    });

    render(<ConnectionsPage />);

    expect(
      screen.getByText("Sign in to manage connections")
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Sign in" })).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: "Create account" })
    ).toBeInTheDocument();
  });

  it("clears auth on 401 error", async () => {
    vi.mocked(getConnections).mockRejectedValue(
      new ApiError("Unauthorized", 401)
    );

    render(<ConnectionsPage />);

    await waitFor(() => {
      expect(mockClear).toHaveBeenCalled();
    });
  });

  it("sets authenticated on successful query", async () => {
    render(<ConnectionsPage />);

    await waitFor(() => {
      expect(mockSetAuthenticated).toHaveBeenCalledWith("test@example.com");
    });
  });

  it("handles generic start link error", async () => {
    const user = userEvent.setup();
    vi.mocked(startLink).mockRejectedValue(new Error("Network error"));

    render(<ConnectionsPage />);

    await waitFor(() => {
      expect(screen.getByText("Microsoft OneDrive")).toBeInTheDocument();
    });

    const connectButtons = screen.getAllByRole("button", { name: /connect/i });
    // Click the second connect button (Microsoft)
    await user.click(connectButtons[1]);

    await waitFor(() => {
      expect(
        screen.getByText("Unexpected error while starting the link.")
      ).toBeInTheDocument();
    });
  });

  it("handles generic disconnect error", async () => {
    const user = userEvent.setup();
    vi.mocked(disconnect).mockRejectedValue(new Error("Network error"));

    // Clear previous mock and set new one with linked connection
    vi.mocked(getConnections).mockClear();
    vi.mocked(getConnections).mockResolvedValue([
      {
        provider: "Google",
        isLinked: true,
        scopes: ["https://www.googleapis.com/auth/drive.readonly"],
      },
      {
        provider: "Microsoft",
        isLinked: false,
        scopes: [],
      },
    ]);

    render(<ConnectionsPage />);

    await waitFor(() => {
      expect(
        screen.getByRole("button", { name: /disconnect/i })
      ).toBeInTheDocument();
    });

    const disconnectButton = screen.getByRole("button", {
      name: /disconnect/i,
    });
    await user.click(disconnectButton);

    await waitFor(() => {
      expect(
        screen.getByText("Unexpected error while disconnecting the provider.")
      ).toBeInTheDocument();
    });
  });
});
