import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@/test/test-utils";
import userEvent from "@testing-library/user-event";
import { TopNav } from "./TopNav";
import * as authApi from "../api/auth";
import { useAuthStore } from "../state/auth-store";
import { ApiError } from "../api/client";

vi.mock("../api/auth");
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

describe("TopNav", () => {
  let mockClear: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.clearAllMocks();
    mockClear = vi.fn();
  });

  describe("when user is not authenticated", () => {
    beforeEach(() => {
      vi.mocked(useAuthStore).mockImplementation((selector: any) => {
        const mockState = {
          isAuthenticated: false,
          userEmail: null,
          clear: mockClear,
        };
        return selector(mockState);
      });
    });

    it("renders logo link", () => {
      render(<TopNav />);

      const logo = screen.getByRole("link", { name: /storageconnector/i });
      expect(logo).toBeInTheDocument();
      expect(logo).toHaveAttribute("href", "/");
    });

    it("renders login and register links", () => {
      render(<TopNav />);

      expect(
        screen.getByRole("link", { name: /^login$/i })
      ).toBeInTheDocument();
      expect(
        screen.getByRole("link", { name: /^register$/i })
      ).toBeInTheDocument();
    });

    it("does not render authenticated navigation", () => {
      render(<TopNav />);

      expect(
        screen.queryByRole("link", { name: /connections/i })
      ).not.toBeInTheDocument();
      expect(
        screen.queryByRole("link", { name: /files/i })
      ).not.toBeInTheDocument();
      expect(
        screen.queryByRole("link", { name: /account/i })
      ).not.toBeInTheDocument();
      expect(
        screen.queryByRole("button", { name: /logout/i })
      ).not.toBeInTheDocument();
    });
  });

  describe("when user is authenticated", () => {
    beforeEach(() => {
      vi.mocked(useAuthStore).mockImplementation((selector: any) => {
        const mockState = {
          isAuthenticated: true,
          userEmail: "test@example.com",
          clear: mockClear,
        };
        return selector(mockState);
      });
    });

    it("renders logo link", () => {
      render(<TopNav />);

      const logo = screen.getByRole("link", { name: /storageconnector/i });
      expect(logo).toBeInTheDocument();
      expect(logo).toHaveAttribute("href", "/");
    });

    it("renders main navigation links", () => {
      render(<TopNav />);

      const connectionsLink = screen.getByRole("link", {
        name: /connections/i,
      });
      const filesLink = screen.getByRole("link", { name: /files/i });

      expect(connectionsLink).toBeInTheDocument();
      expect(connectionsLink).toHaveAttribute("href", "/connections");

      expect(filesLink).toBeInTheDocument();
      expect(filesLink).toHaveAttribute("href", "/files");
    });

    it("displays user email", () => {
      render(<TopNav />);

      expect(screen.getByText("test@example.com")).toBeInTheDocument();
    });

    it("renders account link", () => {
      render(<TopNav />);

      const accountLink = screen.getByRole("link", { name: /account/i });
      expect(accountLink).toBeInTheDocument();
      expect(accountLink).toHaveAttribute("href", "/account");
    });

    it("renders logout button", () => {
      render(<TopNav />);

      expect(
        screen.getByRole("button", { name: /logout/i })
      ).toBeInTheDocument();
    });

    it("does not render login/register links", () => {
      render(<TopNav />);

      expect(
        screen.queryByRole("link", { name: /^login$/i })
      ).not.toBeInTheDocument();
      expect(
        screen.queryByRole("link", { name: /^register$/i })
      ).not.toBeInTheDocument();
    });

    it("handles successful logout", async () => {
      const user = userEvent.setup();
      vi.mocked(authApi.logout).mockResolvedValueOnce(undefined);

      render(<TopNav />);

      await user.click(screen.getByRole("button", { name: /logout/i }));

      await waitFor(() => {
        expect(authApi.logout).toHaveBeenCalled();
        expect(mockClear).toHaveBeenCalled();
        expect(mockNavigate).toHaveBeenCalledWith("/login");
      });
    });

    it("shows loading state during logout", async () => {
      const user = userEvent.setup();
      vi.mocked(authApi.logout).mockImplementation(
        () => new Promise((resolve) => setTimeout(resolve, 100))
      );

      render(<TopNav />);

      await user.click(screen.getByRole("button", { name: /logout/i }));

      expect(
        screen.getByRole("button", { name: /signing out/i })
      ).toBeDisabled();
    });

    it("handles logout with 401 error", async () => {
      const user = userEvent.setup();
      vi.mocked(authApi.logout).mockRejectedValueOnce(
        new ApiError("Unauthorized", 401)
      );

      render(<TopNav />);

      await user.click(screen.getByRole("button", { name: /logout/i }));

      await waitFor(() => {
        expect(mockClear).toHaveBeenCalled();
        expect(mockNavigate).toHaveBeenCalledWith("/login");
      });
    });
  });

  it("has header element with correct styling", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: false,
        userEmail: null,
        clear: mockClear,
      };
      return selector(mockState);
    });

    const { container } = render(<TopNav />);
    const header = container.querySelector("header");

    expect(header).toBeInTheDocument();
    expect(header).toHaveClass(
      "border-b",
      "border-slate-200",
      "bg-white/80",
      "backdrop-blur"
    );
  });

  it("has max-width constraint on content", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: false,
        userEmail: null,
        clear: mockClear,
      };
      return selector(mockState);
    });

    const { container } = render(<TopNav />);
    const contentDiv = container.querySelector("div[style]");

    expect(contentDiv).toHaveStyle({ maxWidth: "1650px" });
  });
});
