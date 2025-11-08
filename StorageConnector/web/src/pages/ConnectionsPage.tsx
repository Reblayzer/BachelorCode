import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { disconnect, getConnections, startLink } from "../api/connections";
import { ApiError } from "../api/client";
import type { ProviderType } from "../api/types";
import { PROVIDER_META, PROVIDER_ORDER } from "../constants/providers";
import { useAuthStore } from "../state/auth-store";
import {
  Alert,
  Button,
  Card,
  PageContainer,
  PageHeader,
  PageSection,
} from "../components/ui";

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
          `Unable to start the ${provider} link: ${error.message}`
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
        setActionError(`Unable to disconnect ${provider}: ${error.message}`);
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
      <PageContainer maxWidth="md">
        <Card className="p-8 text-center space-y-6">
          <h1 className="text-3xl font-bold text-slate-900">
            Sign in to manage connections
          </h1>
          <p className="text-slate-600">
            We use your StorageConnector session to secure provider links.
            Please sign in, then connect Google or Microsoft accounts.
          </p>
          <div className="flex justify-center gap-3">
            <Link to="/login">
              <Button variant="primary">Sign in</Button>
            </Link>
            <Link to="/register">
              <Button variant="secondary">Create account</Button>
            </Link>
          </div>
        </Card>
      </PageContainer>
    );
  }

  const isLoading =
    connectionsQuery.isLoading || connectionsQuery.isFetching || false;

  const connectionsByProvider = new Map(
    (connectionsQuery.data ?? []).map((connection) => [
      connection.provider,
      connection,
    ])
  );

  return (
    <PageContainer>
      <PageSection>
        <PageHeader
          title="Your connections"
          description="Link your Google and Microsoft accounts. We will display the scopes granted and let you revoke access at any time."
        />
        {actionMessage && <Alert variant="success">{actionMessage}</Alert>}
        {actionError && <Alert variant="error">{actionError}</Alert>}
        <div className="grid gap-4 md:grid-cols-2">
          {PROVIDER_ORDER.map((provider) => {
            const connection = connectionsByProvider.get(provider);
            const meta = PROVIDER_META[provider];
            const linked = connection?.isLinked ?? false;
            const scopes = connection?.scopes ?? [];

            return (
              <Card key={provider} className="flex flex-col p-6 h-full">
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
                <div className="flex flex-wrap gap-2 mb-auto">
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
                  <Button
                    variant="primary"
                    onClick={() => {
                      setActionMessage(null);
                      startLinkMutation.mutate(provider);
                    }}
                    className="flex-1"
                    disabled={startLinkMutation.isPending || isLoading}
                    isLoading={startLinkMutation.isPending}
                  >
                    {linked ? "Reconnect" : "Connect"}
                  </Button>
                  {linked && (
                    <Button
                      variant="danger"
                      onClick={() => {
                        setActionMessage(null);
                        disconnectMutation.mutate(provider);
                      }}
                      disabled={disconnectMutation.isPending}
                      isLoading={disconnectMutation.isPending}
                    >
                      Disconnect
                    </Button>
                  )}
                </div>
              </Card>
            );
          })}
        </div>
      </PageSection>
    </PageContainer>
  );
};
