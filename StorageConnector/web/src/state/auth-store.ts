import { create } from "zustand";

type AuthState = {
  isAuthenticated: boolean;
  userEmail?: string;
  setAuthenticated: (email?: string) => void;
  clear: () => void;
};

export const useAuthStore = create<AuthState>((set) => ({
  isAuthenticated: false,
  userEmail: undefined,
  setAuthenticated: (email) =>
    set((state) => ({
      isAuthenticated: true,
      userEmail: email ?? state.userEmail,
    })),
  clear: () => set({ isAuthenticated: false, userEmail: undefined }),
}));
