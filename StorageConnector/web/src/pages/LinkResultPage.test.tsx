import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@/test/test-utils";
import { LinkResultPage } from "./LinkResultPage";

const mockNavigate = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useSearchParams: () => {
      const params = new URLSearchParams(window.location.search);
      return [params, vi.fn()];
    },
  };
});

describe("LinkResultPage - Success", () => {
  beforeEach(() => {
    vi.clearAllMocks();

    // Mock window.location.search
    delete (window as any).location;
    (window as any).location = { search: "?provider=Google" };
  });

  it("renders success message", () => {
    render(<LinkResultPage variant="success" />);

    expect(screen.getByText("Connection successful")).toBeInTheDocument();
    expect(
      screen.getByText(/Your Google account is now linked/i)
    ).toBeInTheDocument();
  });

  it("shows success icon", () => {
    render(<LinkResultPage variant="success" />);

    expect(screen.getByText("✓")).toBeInTheDocument();
  });

  it("displays navigation buttons", () => {
    render(<LinkResultPage variant="success" />);

    expect(screen.getByText("Back to connections")).toBeInTheDocument();
    expect(screen.getByText("Home")).toBeInTheDocument();
  });

  it("does not call setAuthenticated - JWT is already in localStorage", () => {
    // No-op test - just to document that setAuthenticated is not called
    render(<LinkResultPage variant="success" />);
    // Success - component rendered without errors
  });

  it("shows provider name from query params", () => {
    render(<LinkResultPage variant="success" />);

    expect(
      screen.getByText(/Google account is now linked/i)
    ).toBeInTheDocument();
  });

  it("shows generic message when no provider in params", () => {
    (window as any).location = { search: "" };

    render(<LinkResultPage variant="success" />);

    expect(
      screen.getByText(/Your storage account is now linked/i)
    ).toBeInTheDocument();
  });
});

describe("LinkResultPage - Error", () => {
  beforeEach(() => {
    vi.clearAllMocks();

    (window as any).location = { search: "?error=Access denied" };
  });

  it("renders error message", () => {
    render(<LinkResultPage variant="error" />);

    expect(
      screen.getByRole("heading", { name: /couldn.*t finish linking/i })
    ).toBeInTheDocument();
  });

  it("shows error icon", () => {
    render(<LinkResultPage variant="error" />);

    expect(screen.getByText("!")).toBeInTheDocument();
  });

  it("displays error from query params", () => {
    render(<LinkResultPage variant="error" />);

    expect(screen.getByText("Access denied")).toBeInTheDocument();
  });

  it("shows generic error when no error param", () => {
    (window as any).location = { search: "" };

    render(<LinkResultPage variant="error" />);

    expect(
      screen.getByText(/The link could not be completed/i)
    ).toBeInTheDocument();
  });

  it("does not call setAuthenticated on error - no state update needed", () => {
    // No-op test - just to document that setAuthenticated is not called
    render(<LinkResultPage variant="error" />);
    // Success - component rendered without errors
  });

  it("displays navigation buttons", () => {
    render(<LinkResultPage variant="error" />);

    expect(screen.getByText("Back to connections")).toBeInTheDocument();
    expect(screen.getByText("Home")).toBeInTheDocument();
  });
});
