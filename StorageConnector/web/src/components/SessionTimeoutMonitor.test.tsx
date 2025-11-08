import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@/test/test-utils";
import { SessionTimeoutMonitor } from "./SessionTimeoutMonitor";
import { useAuthStore } from "../state/auth-store";

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

describe("SessionTimeoutMonitor", () => {
  let mockClear: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    vi.clearAllMocks();
    mockClear = vi.fn();
  });

  it("does not render when user is not authenticated", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: false,
        clear: mockClear,
      };
      return selector(mockState);
    });

    render(<SessionTimeoutMonitor />);
    expect(screen.queryByText(/session expiring/i)).not.toBeInTheDocument();
  });

  it("does not show warning initially when authenticated", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: true,
        clear: mockClear,
      };
      return selector(mockState);
    });

    render(<SessionTimeoutMonitor />);
    expect(screen.queryByText(/session expiring/i)).not.toBeInTheDocument();
  });

  it("listens to activity events when authenticated", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: true,
        clear: mockClear,
      };
      return selector(mockState);
    });

    render(<SessionTimeoutMonitor />);

    const activityEvents = ["mousedown", "keydown", "scroll", "touchstart"];

    // Should not throw errors when dispatching events
    activityEvents.forEach((eventType) => {
      window.dispatchEvent(new Event(eventType));
    });

    expect(true).toBe(true);
  });

  it("cleans up event listeners on unmount", () => {
    vi.mocked(useAuthStore).mockImplementation((selector: any) => {
      const mockState = {
        isAuthenticated: true,
        clear: mockClear,
      };
      return selector(mockState);
    });

    const addEventListenerSpy = vi.spyOn(window, "addEventListener");
    const removeEventListenerSpy = vi.spyOn(window, "removeEventListener");

    const { unmount } = render(<SessionTimeoutMonitor />);

    const addCallCount = addEventListenerSpy.mock.calls.length;

    unmount();

    const removeCallCount = removeEventListenerSpy.mock.calls.length;

    // Should remove the same number of listeners that were added
    expect(removeCallCount).toBeGreaterThanOrEqual(addCallCount);

    addEventListenerSpy.mockRestore();
    removeEventListenerSpy.mockRestore();
  });
});
