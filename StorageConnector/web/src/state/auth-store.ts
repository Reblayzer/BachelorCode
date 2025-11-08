import { create } from "zustand";

const TOKEN_KEY = "auth_token";

type AuthState = {
  isAuthenticated: boolean;
  userEmail?: string;
  token?: string;
  setAuthenticated: (email: string, token: string) => void;
  clear: () => void;
  getToken: () => string | null;
};

// Initialize from localStorage
const storedToken = localStorage.getItem(TOKEN_KEY);

export const useAuthStore = create<AuthState>((set, get) => ({
  isAuthenticated: !!storedToken,
  userEmail: undefined,
  token: storedToken ?? undefined,
  setAuthenticated: (email, token) => {
    localStorage.setItem(TOKEN_KEY, token);
    set({
      isAuthenticated: true,
      userEmail: email,
      token,
    });
  },
  clear: () => {
    localStorage.removeItem(TOKEN_KEY);
    set({ isAuthenticated: false, userEmail: undefined, token: undefined });
  },
  getToken: () => get().token ?? null,
}));
