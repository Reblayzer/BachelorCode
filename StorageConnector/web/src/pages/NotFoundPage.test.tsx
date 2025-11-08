import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@/test/test-utils";
import { NotFoundPage } from "./NotFoundPage";

const mockUseRouteError = vi.fn();
vi.mock("react-router-dom", async () => {
  const actual = await vi.importActual("react-router-dom");
  return {
    ...actual,
    useRouteError: () => mockUseRouteError(),
    isRouteErrorResponse: (error: any) => error?.status === 404,
  };
});

describe("NotFoundPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders 404 error message", () => {
    mockUseRouteError.mockReturnValue({ status: 404 });

    render(<NotFoundPage />);

    expect(screen.getByText("Page not found")).toBeInTheDocument();
    expect(
      screen.getByText(/We couldn.*t find that page/i)
    ).toBeInTheDocument();
  });

  it("renders generic error message for non-404 errors", () => {
    mockUseRouteError.mockReturnValue({ status: 500 });

    render(<NotFoundPage />);

    expect(screen.getByText("Something went wrong")).toBeInTheDocument();
    expect(
      screen.getByText("An unexpected error occurred.")
    ).toBeInTheDocument();
  });

  it("renders go home button", () => {
    mockUseRouteError.mockReturnValue({ status: 404 });

    render(<NotFoundPage />);

    const homeButton = screen.getByText("Go home");
    expect(homeButton).toBeInTheDocument();
    expect(homeButton.closest("a")).toHaveAttribute("href", "/");
  });

  it("handles error objects without status", () => {
    mockUseRouteError.mockReturnValue(new Error("Something went wrong"));

    render(<NotFoundPage />);

    expect(screen.getByText("Something went wrong")).toBeInTheDocument();
    expect(
      screen.getByText("An unexpected error occurred.")
    ).toBeInTheDocument();
  });
});
