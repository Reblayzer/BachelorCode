import { useEffect, type ReactNode } from "react";
import { getCurrentUser } from "../api/auth";
import { ApiError } from "../api/client";
import { useAuthStore } from "./auth-store";

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  useEffect(() => {
    const probe = async () => {
      try {
        // If we have a token, verify it by fetching current user
        const token = useAuthStore.getState().getToken();
        if (!token) {
          useAuthStore.getState().clear();
          return;
        }

        const response = await getCurrentUser();
        if (response?.email) {
          // Update email but keep existing token
          useAuthStore.getState().setAuthenticated(response.email, token);
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          useAuthStore.getState().clear();
        }
      }
    };

    void probe();
  }, []);

  return <>{children}</>;
};
