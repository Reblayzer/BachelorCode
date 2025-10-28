import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Link, NavLink, useNavigate } from "react-router-dom";
import { useAuthStore } from "../state/auth-store";
import { logout } from "../api/auth";
import { ApiError } from "../api/client";

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  [
    "rounded-md px-3 py-2 text-sm font-medium transition-colors",
    isActive
      ? "bg-slate-200 text-slate-900"
      : "text-slate-600 hover:bg-slate-100 hover:text-slate-900",
  ].join(" ");

export const TopNav = () => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const userEmail = useAuthStore((state) => state.userEmail);
  const clear = useAuthStore((state) => state.clear);
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const logoutMutation = useMutation({
    mutationFn: logout,
    onSuccess: () => {
      clear();
      queryClient.removeQueries({ queryKey: ["connections"], exact: false });
      navigate("/login");
    },
    onError: (error: unknown) => {
      if (error instanceof ApiError && error.status === 401) {
        clear();
        navigate("/login");
      }
    },
  });

  const handleLogout = () => {
    logoutMutation.mutate();
  };

  return (
    <header className="border-b border-slate-200 bg-white/80 backdrop-blur">
      <div className="mx-auto flex h-16 w-full max-w-5xl items-center justify-between px-4">
        <Link to="/" className="text-lg font-semibold text-brand">
          StorageConnector
        </Link>

        <nav className="flex items-center gap-2">
          <NavLink to="/connections" className={navLinkClass}>
            Connections
          </NavLink>
          {isAuthenticated ? (
            <div className="flex items-center gap-3">
              <span className="text-sm text-slate-500">{userEmail}</span>
              <button
                type="button"
                onClick={handleLogout}
                className="rounded-md bg-slate-900 px-3 py-2 text-sm font-semibold text-white transition hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-70"
                disabled={logoutMutation.isPending}
              >
                {logoutMutation.isPending ? "Signing out…" : "Logout"}
              </button>
            </div>
          ) : (
            <>
              <NavLink to="/login" className={navLinkClass}>
                Login
              </NavLink>
              <NavLink to="/register" className={navLinkClass}>
                Register
              </NavLink>
            </>
          )}
        </nav>
      </div>
    </header>
  );
};
