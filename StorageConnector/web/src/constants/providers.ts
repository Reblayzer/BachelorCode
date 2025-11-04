import type { ProviderType } from "../api/types";

type ProviderMeta = {
  name: string;
  description: string;
  accent: string;
  icon: string;
  color: string;
};

export const PROVIDER_META: Record<ProviderType, ProviderMeta> = {
  Google: {
    name: "Google Drive",
    description: "Connect drive data with read-only access.",
    accent: "from-[#4285F4] to-[#0F9D58]",
    icon: "📁",
    color: "#4285F4",
  },
  Microsoft: {
    name: "Microsoft OneDrive",
    description: "Read file metadata from your OneDrive tenant.",
    accent: "from-[#0A66C2] to-[#7FBA00]",
    icon: "☁️",
    color: "#0A66C2",
  },
};

export const PROVIDER_ORDER: ProviderType[] = ["Google", "Microsoft"];
