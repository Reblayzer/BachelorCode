export class ApiError extends Error {
  status: number;
  data?: unknown;

  constructor(message: string, status: number, data?: unknown) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.data = data;
  }
}

export type ApiRequestInit = Omit<RequestInit, "body" | "headers"> & {
  body?: unknown;
  headers?: Record<string, string>;
  /**
   * Optional client-side timeout (ms). If provided, the request is aborted after this duration.
   * To use your own AbortSignal, pass `signal` instead.
   */
  timeoutMs?: number;
};

const apiBase =
  (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(
    /\/$/,
    "",
  );

// Legacy: Direct service URLs (fallback when API Gateway not used)
const identityBase =
  (import.meta.env.VITE_IDENTITY_BASE_URL as string | undefined)?.replace(
    /\/$/,
    "",
  );
const linkingBase =
  (import.meta.env.VITE_LINKING_BASE_URL as string | undefined)?.replace(
    /\/$/,
    "",
  );

const defaultHeaders = {
  Accept: "application/json",
  "X-Api-Version": "1.0", // API versioning support
};

// Get token from localStorage
const getAuthToken = (): string | null => {
  return localStorage.getItem("auth_token");
};

const resolveUrl = (base: string | undefined, path: string) =>
  base ? `${base}${path}` : path;

const toJson = (input: Response) => input.json() as Promise<unknown>;
const DEFAULT_TIMEOUT_MS = 30000;

async function request<TResponse>(
  baseUrl: string | undefined,
  path: string,
  init: ApiRequestInit = {},
): Promise<TResponse> {
  const { timeoutMs, signal, ...rest } = init;

  const headers: Record<string, string> = {
    ...defaultHeaders,
    ...(rest.headers ?? {}),
  };

  // Add Authorization header if token exists
  const token = getAuthToken();
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  let body: BodyInit | undefined;

  if (rest.body instanceof FormData) {
    body = rest.body;
  } else if (rest.body !== undefined) {
    headers["Content-Type"] = "application/json";
    body = JSON.stringify(rest.body);
  }

  // If caller provided a signal, prefer it and skip timeout handling to avoid double-aborts.
  const controller =
    !signal && (timeoutMs ?? DEFAULT_TIMEOUT_MS) > 0
      ? new AbortController()
      : undefined;
  const abortSignal = signal ?? controller?.signal;
  const timeoutId =
    controller && (timeoutMs ?? DEFAULT_TIMEOUT_MS)
      ? setTimeout(() => controller.abort(), timeoutMs ?? DEFAULT_TIMEOUT_MS)
      : undefined;

  const response = await fetch(resolveUrl(baseUrl, path), {
    ...rest,
    headers,
    body,
    credentials: "include",
    signal: abortSignal,
  });

  if (timeoutId) {
    clearTimeout(timeoutId);
  }

  const contentType = response.headers.get("content-type") ?? "";
  const hasJson = contentType.includes("application/json");
  let payload: unknown = undefined;

  if (response.status !== 204 && response.status !== 205) {
    if (hasJson) {
      payload = await toJson(response).catch(() => undefined);
    } else {
      const text = await response.text();
      payload = text.length ? text : undefined;
    }
  }

  if (!response.ok) {
    // Handle 401 Unauthorized - clear token and redirect to login
    if (response.status === 401) {
      localStorage.removeItem("auth_token");
      // Redirect to login page only if not already on login page
      if (window.location.pathname !== "/login") {
        window.location.href = "/login";
      }
    }

    const message =
      (typeof payload === "object" &&
        payload !== null &&
        "message" in payload &&
        typeof (payload as { message: unknown }).message === "string" &&
        (payload as { message: string }).message) ||
      response.statusText ||
      "Request failed";

    throw new ApiError(message, response.status, payload);
  }

  return payload as TResponse;
}

// Use API Gateway if configured, otherwise fall back to direct service URLs
export const identityRequest = <TResponse>(
  path: string,
  init?: ApiRequestInit,
) => request<TResponse>(apiBase || identityBase, path, init);

export const linkingRequest = <TResponse>(
  path: string,
  init?: ApiRequestInit,
) => request<TResponse>(apiBase || linkingBase, path, init);
