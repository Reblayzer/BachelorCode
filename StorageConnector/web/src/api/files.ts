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
  return await linkingRequest<ProviderFileItem[]>("/api/files");
}

export async function getFileMetadata(provider: ProviderType, fileId: string): Promise<FileMetadata> {
  return await linkingRequest<FileMetadata>(`/api/files/${provider}/${fileId}/metadata`);
}
