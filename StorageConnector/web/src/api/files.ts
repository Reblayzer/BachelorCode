import { linkingRequest } from "./client";
import type { ProviderType } from "./types";

export interface ProviderFileItem {
  id: string;
  name: string;
  mimeType: string | null;
  sizeBytes: number | null;
  modifiedUtc: string;
  provider: "Google" | "Microsoft";
}

export interface FileMetadata {
  id: string;
  name: string;
  mimeType: string | null;
  sizeBytes: number | null;
  modifiedUtc: string | null;
  providerSpecificJson: string | null;
}

export async function getFiles(): Promise<ProviderFileItem[]> {
  return await linkingRequest<ProviderFileItem[]>("/api/v1/files");
}

export async function getFileMetadata(provider: ProviderType, fileId: string): Promise<FileMetadata> {
  return await linkingRequest<FileMetadata>(`/api/v1/files/${provider}/${fileId}/metadata`);
}

/**
 * Opens a file in the provider's web interface (Google Drive or OneDrive).
 * This function fetches the view URL from the backend and opens it in a new tab.
 */
export async function openFileInProvider(provider: ProviderType, fileId: string): Promise<void> {
  // Call the backend endpoint that returns the view URL as JSON
  const response = await linkingRequest<{ url: string }>(`/api/v1/files/${provider}/${fileId}/view-url`);

  // Open the URL in a new tab
  window.open(response.url, "_blank");
}

