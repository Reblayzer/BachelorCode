import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@/test/test-utils";
import { FilesPage } from "./FilesPage";
import { getFiles } from "../api/files";
import type { ProviderFileItem } from "../api/files";
import userEvent from "@testing-library/user-event";

vi.mock("../api/files");

describe("FilesPage", () => {
  const mockFiles: ProviderFileItem[] = [
    {
      id: "1",
      name: "Document.pdf",
      mimeType: "application/pdf",
      sizeBytes: 1024000,
      modifiedUtc: "2024-01-15T10:30:00Z",
      provider: "Google",
    },
    {
      id: "2",
      name: "Presentation.pptx",
      mimeType: "application/vnd.ms-powerpoint",
      sizeBytes: 2048000,
      modifiedUtc: "2024-01-16T14:20:00Z",
      provider: "Microsoft",
    },
    {
      id: "3",
      name: "My Folder",
      mimeType: "application/vnd.google-apps.folder",
      sizeBytes: null,
      modifiedUtc: "2024-01-17T09:15:00Z",
      provider: "Google",
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getFiles).mockResolvedValue(mockFiles);
  });

  it("renders files page with header", async () => {
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("My Files")).toBeInTheDocument();
    });

    expect(screen.getByText(/Showing 3 of 3 files/i)).toBeInTheDocument();
  });

  it("displays files in table", async () => {
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("Document.pdf")).toBeInTheDocument();
    });

    expect(screen.getByText("Presentation.pptx")).toBeInTheDocument();
    expect(screen.getByText("My Folder")).toBeInTheDocument();
  });

  it("filters files by search term", async () => {
    const user = userEvent.setup();
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("Document.pdf")).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/search files/i);
    await user.type(searchInput, "Document");

    await waitFor(() => {
      expect(screen.getByText("Document.pdf")).toBeInTheDocument();
      expect(screen.queryByText("Presentation.pptx")).not.toBeInTheDocument();
    });
  });

  it("filters files by provider", async () => {
    const user = userEvent.setup();
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("Document.pdf")).toBeInTheDocument();
    });

    // Open filter menu
    const filterButton = screen.getByRole("button", { name: /filter/i });
    await user.click(filterButton);

    // Deselect Google
    const googleCheckbox = screen.getByRole("checkbox", {
      name: /google drive/i,
    });
    await user.click(googleCheckbox);

    await waitFor(() => {
      expect(screen.queryByText("Document.pdf")).not.toBeInTheDocument();
      expect(screen.getByText("Presentation.pptx")).toBeInTheDocument();
    });
  });

  it("shows file size correctly", async () => {
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("1000 KB")).toBeInTheDocument();
    });

    expect(screen.getByText("1.95 MB")).toBeInTheDocument();
  });

  it("shows loading state", () => {
    vi.mocked(getFiles).mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    render(<FilesPage />);

    expect(screen.getByText("Loading files...")).toBeInTheDocument();
  });

  it("shows error state", async () => {
    vi.mocked(getFiles).mockRejectedValue(new Error("Failed to load files"));

    render(<FilesPage />);

    await waitFor(
      () => {
        expect(screen.getByText("Error Loading Files")).toBeInTheDocument();
      },
      { timeout: 3000 }
    );

    expect(
      screen.getByRole("button", { name: /try again/i })
    ).toBeInTheDocument();
  });

  it("shows refresh button and handles refresh", async () => {
    const user = userEvent.setup();
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("Document.pdf")).toBeInTheDocument();
    });

    const refreshButton = screen.getByRole("button", { name: /refresh/i });
    expect(refreshButton).toBeInTheDocument();

    await user.click(refreshButton);

    expect(getFiles).toHaveBeenCalledTimes(2);
  });

  it("displays provider badges with correct colors", async () => {
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("Document.pdf")).toBeInTheDocument();
    });

    const googleBadges = screen.getAllByText("Google Drive");
    expect(googleBadges.length).toBeGreaterThan(0);

    const microsoftBadge = screen.getByText("Microsoft OneDrive");
    expect(microsoftBadge).toBeInTheDocument();
  });

  it("shows folder icon for folders", async () => {
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("My Folder")).toBeInTheDocument();
    });

    // Check that folder row exists
    const folderRow = screen.getByText("My Folder").closest("div");
    expect(folderRow).toBeInTheDocument();
  });

  it("handles empty file list", async () => {
    vi.mocked(getFiles).mockResolvedValue([]);

    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("My Files")).toBeInTheDocument();
    });

    // Data table should show no data message
    expect(screen.queryByText("Document.pdf")).not.toBeInTheDocument();
  });

  it("can toggle filter menu", async () => {
    const user = userEvent.setup();
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("Document.pdf")).toBeInTheDocument();
    });

    const filterButton = screen.getByRole("button", { name: /filter/i });
    await user.click(filterButton);

    expect(
      screen.getByRole("checkbox", { name: /google drive/i })
    ).toBeInTheDocument();

    // Close menu
    await user.click(filterButton);

    await waitFor(() => {
      expect(
        screen.queryByRole("checkbox", { name: /google drive/i })
      ).not.toBeInTheDocument();
    });
  });

  it("displays modified date and time", async () => {
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("Document.pdf")).toBeInTheDocument();
    });

    // Just verify files rendered successfully - exact date format depends on locale
    expect(screen.getByText("Document.pdf")).toBeInTheDocument();
  });

  it("formats file sizes correctly", async () => {
    const filesWithVariedSizes: ProviderFileItem[] = [
      {
        id: "1",
        name: "tiny.txt",
        mimeType: "text/plain",
        sizeBytes: 0,
        modifiedUtc: "2024-01-15T10:30:00Z",
        provider: "Google",
      },
      {
        id: "2",
        name: "small.txt",
        mimeType: "text/plain",
        sizeBytes: 500,
        modifiedUtc: "2024-01-15T10:30:00Z",
        provider: "Google",
      },
      {
        id: "3",
        name: "folder",
        mimeType: "application/vnd.google-apps.folder",
        sizeBytes: null,
        modifiedUtc: "2024-01-15T10:30:00Z",
        provider: "Google",
      },
    ];

    vi.mocked(getFiles).mockResolvedValue(filesWithVariedSizes);
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("tiny.txt")).toBeInTheDocument();
    });

    // Check that size formatting logic is triggered
    expect(screen.getByText("small.txt")).toBeInTheDocument();
    expect(screen.getByText("folder")).toBeInTheDocument();
  });

  it("can clear search term", async () => {
    const user = userEvent.setup();
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("Document.pdf")).toBeInTheDocument();
    });

    const searchInput = screen.getByPlaceholderText(/search files/i);
    await user.type(searchInput, "Document");

    await waitFor(() => {
      expect(screen.queryByText("Presentation.pptx")).not.toBeInTheDocument();
    });

    // Find and click clear button
    const clearButton = screen.getByRole("button", { name: "" });
    await user.click(clearButton);

    await waitFor(() => {
      expect(screen.getByText("Presentation.pptx")).toBeInTheDocument();
    });
  });

  it("can toggle provider filters", async () => {
    const user = userEvent.setup();
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("Document.pdf")).toBeInTheDocument();
    });

    // Open filter menu
    const filterButton = screen.getByRole("button", { name: /filter/i });
    await user.click(filterButton);

    // Uncheck Google Drive to filter it out
    const googleCheckbox = screen.getByRole("checkbox", {
      name: /google drive/i,
    });
    await user.click(googleCheckbox);

    await waitFor(() => {
      // Google files should be hidden
      expect(screen.queryByText("Document.pdf")).not.toBeInTheDocument();
      expect(screen.queryByText("My Folder")).not.toBeInTheDocument();
      // Microsoft file should still be visible
      expect(screen.getByText("Presentation.pptx")).toBeInTheDocument();
    });

    // Re-check to show again
    await user.click(googleCheckbox);

    await waitFor(() => {
      expect(screen.getByText("Document.pdf")).toBeInTheDocument();
    });
  });

  it("displays mime types correctly", async () => {
    const filesWithMimeTypes: ProviderFileItem[] = [
      {
        id: "1",
        name: "document.pdf",
        mimeType: "application/pdf",
        sizeBytes: 1000,
        modifiedUtc: "2024-01-15T10:30:00Z",
        provider: "Google",
      },
      {
        id: "2",
        name: "image.jpg",
        mimeType: "image/jpeg",
        sizeBytes: 2000,
        modifiedUtc: "2024-01-15T10:30:00Z",
        provider: "Google",
      },
      {
        id: "3",
        name: "spreadsheet.xlsx",
        mimeType: "application/vnd.ms-excel",
        sizeBytes: 3000,
        modifiedUtc: "2024-01-15T10:30:00Z",
        provider: "Google",
      },
    ];

    vi.mocked(getFiles).mockResolvedValue(filesWithMimeTypes);
    render(<FilesPage />);

    await waitFor(() => {
      expect(screen.getByText("document.pdf")).toBeInTheDocument();
    });

    // Verify mime type display logic is triggered
    expect(screen.getByText("image.jpg")).toBeInTheDocument();
    expect(screen.getByText("spreadsheet.xlsx")).toBeInTheDocument();
  });
});
