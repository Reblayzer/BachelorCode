import { linkingRequest } from "./client";
import type {
  ConnectionStatus,
  ProviderType,
  RedirectResponse,
} from "./types";

export const getConnections = () =>
  linkingRequest<ConnectionStatus[]>("/api/connections/status", {
    method: "GET",
  });

export const startLink = (provider: ProviderType) =>
  linkingRequest<RedirectResponse>(`/api/connect/${provider}/start`, {
    method: "GET",
  });

export const disconnect = (provider: ProviderType) =>
  linkingRequest<void>(`/api/connect/${provider}/disconnect`, {
    method: "POST",
  });
