import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { disconnect, getConnections, startLink } from "../api/connections";
import { ApiError } from "../api/client";
import type { ProviderType } from "../api/types";
import { PROVIDER_META, PROVIDER_ORDER } from "../constants/providers";
import { useAuthStore } from "../state/auth-store";

export const ConnectionsPage = () => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const setAuthenticated = useAuthStore((state) => state.setAuthenticated);
  const clear = useAuthStore((state) => state.clear);
  const userEmail = useAuthStore((state) => state.userEmail);
  const queryClient = useQueryClient();
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionMessage, setActionMessage] = useState<string | null>(null);

  const connectionsQuery = useQuery({
    queryKey: ["connections"],
    queryFn: getConnections,
    retry: false,
  });

  useEffect(() => {
    if (connectionsQuery.isSuccess) {
      setAuthenticated(userEmail);
    }
  }, [connectionsQuery.isSuccess, setAuthenticated, userEmail]);

  useEffect(() => {
    if (
      connectionsQuery.error instanceof ApiError &&
      connectionsQuery.error.status === 401
    ) {
      clear();
    }
  }, [connectionsQuery.error, clear]);

  const startLinkMutation = useMutation({
    mutationFn: startLink,
    onSuccess: (response) => {
      setActionError(null);
      setActionMessage("Redirecting to provider…");
      window.location.href = response.redirectUrl;
    },
    onError: (error: unknown, provider: ProviderType) => {
      if (error instanceof ApiError) {
        setActionError(
          `Unable to start the ${provider} link: ${error.message}`,
        );
        return;
      }
      setActionError("Unexpected error while starting the link.");
    },
  });

  const disconnectMutation = useMutation({
    mutationFn: disconnect,
    onSuccess: async (_, provider) => {
      setActionError(null);
      setActionMessage(`${provider} disconnected.`);
      await queryClient.invalidateQueries({ queryKey: ["connections"] });
    },
    onError: (error: unknown, provider: ProviderType) => {
      if (error instanceof ApiError) {
        setActionError(
          `Unable to disconnect ${provider}: ${error.message}`,
        );
        return;
      }
      setActionError("Unexpected error while disconnecting the provider.");
    },
  });

  if (
    !isAuthenticated ||
    (connectionsQuery.error instanceof ApiError &&
      connectionsQuery.error.status === 401)
  ) {
    return (
      <section className="mx-auto max-w-2xl space-y-6 rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm">
        <h1 className="text-3xl font-bold text-slate-900">
          Sign in to manage connections
        </h1>
        <p className="text-slate-600">
          We use your StorageConnector session to secure provider links. Please
          sign in, then connect Google or Microsoft accounts.
        </p>
        <div className="flex justify-center gap-3">
          <Link
            to="/login"
            className="rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white hover:bg-brand-dark"
          >
            Sign in
          </Link>
          <Link
            to="/register"
            className="rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
          >
            Create account
          </Link>
        </div>
      </section>
    );
  }

  const isLoading =
    connectionsQuery.isLoading || connectionsQuery.isFetching || false;

  const connectionsByProvider = new Map(
    (connectionsQuery.data ?? []).map((connection) => [
      connection.provider,
      connection,
    ]),
  );

  return (
    <section className="space-y-8">
      <header className="space-y-2">
        <h1 className="text-3xl font-bold text-slate-900">Your connections</h1>
        <p className="text-sm text-slate-600">
          Link your Google and Microsoft accounts. We will display the scopes
          granted and let you revoke access at any time.
        </p>
      </header>

      {(actionMessage || actionError) && (
        <div
          className={`rounded-md border px-4 py-3 text-sm ${
            actionError
              ? "border-red-200 bg-red-50 text-red-600"
              : "border-emerald-200 bg-emerald-50 text-emerald-700"
          }`}
        >
          {actionError ?? actionMessage}
        </div>
      )}

      <div className="grid gap-4 md:grid-cols-2">
        {PROVIDER_ORDER.map((provider) => {
          const connection = connectionsByProvider.get(provider);
          const meta = PROVIDER_META[provider];
          const linked = connection?.isLinked ?? false;
          const scopes = connection?.scopes ?? [];

          return (
            <article
              key={provider}
              className="flex flex-col rounded-2xl border border-slate-200 bg-white p-6 shadow-sm"
            >
              <header className="mb-4 flex items-center justify-between">
                <div>
                  <h2 className="text-lg font-semibold text-slate-900">
                    {meta.name}
                  </h2>
                  <p className="text-sm text-slate-500">{meta.description}</p>
                </div>
                <span
                  className={`rounded-full px-3 py-1 text-xs font-semibold ${
                    linked
                      ? "bg-emerald-50 text-emerald-600"
                      : "bg-slate-100 text-slate-500"
                  }`}
                >
                  {linked ? "Linked" : "Not linked"}
                </span>
              </header>

              <div className="flex flex-wrap gap-2">
                {scopes.length === 0 && (
                  <span className="rounded-md bg-slate-100 px-2 py-1 text-xs text-slate-500">
                    No scopes recorded
                  </span>
                )}
                {scopes.map((scope) => (
                  <span
                    key={scope}
                    className="rounded-md bg-slate-100 px-2 py-1 text-xs text-slate-600"
                  >
                    {scope}
                  </span>
                ))}
              </div>

              <div className="mt-6 flex gap-3">
                <button
                  type="button"
                  onClick={() => {
                    setActionMessage(null);
                    startLinkMutation.mutate(provider);
                  }}
                  className="flex-1 rounded-md bg-brand px-3 py-2 text-sm font-semibold text-white transition hover:bg-brand-dark disabled:cursor-not-allowed disabled:opacity-70"
                  disabled={startLinkMutation.isPending || isLoading}
                >
                  {startLinkMutation.isPending ? "Preparing…" : linked ? "Reconnect" : "Connect"}
                </button>
                {linked && (
                  <button
                    type="button"
                    onClick={() => {
                      setActionMessage(null);
                      disconnectMutation.mutate(provider);
                    }}
                    className="rounded-md border border-red-200 px-3 py-2 text-sm font-semibold text-red-600 transition hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-70"
                    disabled={disconnectMutation.isPending}
                  >
                    {disconnectMutation.isPending ? "Removing…" : "Disconnect"}
                  </button>
                )}
              </div>
            </article>
          );
        })}
      </div>
    </section>
  );
};
