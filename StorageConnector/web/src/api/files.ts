import { linkingRequest } from "./client";

export interface ProviderFileItem {
  id: string;
  name: string;
  mimeType: string | null;
  modifiedUtc: string;
  provider: "Google" | "Microsoft";
}

export async function getFiles(): Promise<ProviderFileItem[]> {
  return await linkingRequest<ProviderFileItem[]>("/api/files");
}
