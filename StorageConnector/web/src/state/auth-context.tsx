import { useEffect, type ReactNode } from "react";
import { getCurrentUser } from "../api/auth";
import { ApiError } from "../api/client";
import { useAuthStore } from "./auth-store";

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  useEffect(() => {
    const probe = async () => {
      try {
        const response = await getCurrentUser();
        if (response?.email) {
          useAuthStore.getState().setAuthenticated(response.email);
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
