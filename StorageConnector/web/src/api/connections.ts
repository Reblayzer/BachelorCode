import { linkingRequest } from "./client";
import type {
  ConnectionStatus,
  ProviderType,
  RedirectResponse,
} from "./types";

export const getConnections = () =>
  linkingRequest<ConnectionStatus[]>("/api/v1/connections/status", {
    method: "GET",
  });

export const startLink = (provider: ProviderType) =>
  linkingRequest<RedirectResponse>(`/api/v1/connect/${provider}/start`, {
    method: "GET",
  });

export const disconnect = (provider: ProviderType) =>
  linkingRequest<void>(`/api/v1/connect/${provider}/disconnect`, {
    method: "POST",
  });
