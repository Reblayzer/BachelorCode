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
};

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
};

// Get token from localStorage
const getAuthToken = (): string | null => {
  return localStorage.getItem("auth_token");
};

const resolveUrl = (base: string | undefined, path: string) =>
  base ? `${base}${path}` : path;

const toJson = (input: Response) => input.json() as Promise<unknown>;

async function request<TResponse>(
  baseUrl: string | undefined,
  path: string,
  init: ApiRequestInit = {},
): Promise<TResponse> {
  const headers: Record<string, string> = {
    ...defaultHeaders,
    ...(init.headers ?? {}),
  };

  // Add Authorization header if token exists
  const token = getAuthToken();
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  let body: BodyInit | undefined;

  if (init.body instanceof FormData) {
    body = init.body;
  } else if (init.body !== undefined) {
    headers["Content-Type"] = "application/json";
    body = JSON.stringify(init.body);
  }

  const response = await fetch(resolveUrl(baseUrl, path), {
    ...init,
    headers,
    body,
    credentials: "include",
  });

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
      // Optionally redirect to login page
      if (window.location.pathname !== "/auth/login") {
        window.location.href = "/auth/login";
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

export const identityRequest = <TResponse>(
  path: string,
  init?: ApiRequestInit,
) => request<TResponse>(identityBase, path, init);

export const linkingRequest = <TResponse>(
  path: string,
  init?: ApiRequestInit,
) => request<TResponse>(linkingBase, path, init);
