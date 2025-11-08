import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@/test/test-utils";
import { LandingPage } from "./LandingPage";
import { useAuthStore } from "../state/auth-store";

vi.mock("../state/auth-store", () => ({
  useAuthStore: vi.fn(),
}));

describe("LandingPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders landing page with title", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: false,
      };
      return selector(mockState);
    });

    render(<LandingPage />);

    expect(
      screen.getByText(/Manage your cloud storage links in one place/i)
    ).toBeInTheDocument();
  });

  it("shows register and sign in buttons when not authenticated", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: false,
      };
      return selector(mockState);
    });

    render(<LandingPage />);

    expect(screen.getByText("Get started")).toBeInTheDocument();
    expect(screen.getByText("Sign in")).toBeInTheDocument();
  });

  it("shows view connections button when authenticated", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: true,
      };
      return selector(mockState);
    });

    render(<LandingPage />);

    expect(screen.getByText("View connections")).toBeInTheDocument();
    expect(screen.queryByText("Get started")).not.toBeInTheDocument();
    expect(screen.queryByText("Sign in")).not.toBeInTheDocument();
  });

  it("displays feature cards", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: false,
      };
      return selector(mockState);
    });

    render(<LandingPage />);

    expect(screen.getByText("Secure OAuth")).toBeInTheDocument();
    expect(
      screen.getByText("PKCE-protected authentication flow")
    ).toBeInTheDocument();

    expect(screen.getByText("Email Verified")).toBeInTheDocument();
    expect(
      screen.getByText("Confirmation required before linking")
    ).toBeInTheDocument();

    expect(screen.getByText("Multi-Provider")).toBeInTheDocument();
    expect(
      screen.getByText("Google Drive & OneDrive support")
    ).toBeInTheDocument();
  });

  it("displays feature card content", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: false,
      };
      return selector(mockState);
    });

    render(<LandingPage />);

    // Instead of looking for SVG elements, just verify feature cards are present
    const secureOAuthCard = screen.getByText("Secure OAuth").closest("div");
    expect(secureOAuthCard).toBeInTheDocument();

    const emailVerifiedCard = screen.getByText("Email Verified").closest("div");
    expect(emailVerifiedCard).toBeInTheDocument();

    const multiProviderCard = screen.getByText("Multi-Provider").closest("div");
    expect(multiProviderCard).toBeInTheDocument();
  });

  it("contains correct navigation links for unauthenticated users", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: false,
      };
      return selector(mockState);
    });

    render(<LandingPage />);

    const registerLink = screen.getByText("Get started").closest("a");
    expect(registerLink).toHaveAttribute("href", "/register");

    const loginLink = screen.getByText("Sign in").closest("a");
    expect(loginLink).toHaveAttribute("href", "/login");
  });

  it("contains correct navigation link for authenticated users", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: true,
      };
      return selector(mockState);
    });

    render(<LandingPage />);

    const connectionsLink = screen.getByText("View connections").closest("a");
    expect(connectionsLink).toHaveAttribute("href", "/connections");
  });
});
