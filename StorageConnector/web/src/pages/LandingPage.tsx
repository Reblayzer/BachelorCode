import { Link } from "react-router-dom";
import { useAuthStore } from "../state/auth-store";

export const LandingPage = () => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  return (
    <div className="mx-auto max-w-4xl">
      <section className="space-y-8 py-12">
        <div className="space-y-6 text-center">
          <h1 className="text-4xl font-bold tracking-tight text-slate-900 sm:text-5xl">
            Manage your cloud storage links in one place.
          </h1>
          <p className="mx-auto max-w-2xl text-lg text-slate-600">
            Link Google and Microsoft storage accounts securely, see what scopes
            you have granted, and disconnect them with a single click.
          </p>
          <div className="flex flex-wrap justify-center gap-3">
            {isAuthenticated ? (
              <Link
                to="/connections"
                className="rounded-md bg-brand px-6 py-3 text-sm font-semibold text-white shadow hover:bg-brand-dark"
              >
                View connections
              </Link>
            ) : (
              <>
                <Link
                  to="/register"
                  className="rounded-md bg-brand px-6 py-3 text-sm font-semibold text-white shadow hover:bg-brand-dark"
                >
                  Get started
                </Link>
                <Link
                  to="/login"
                  className="rounded-md border border-slate-300 px-6 py-3 text-sm font-semibold text-slate-700 hover:bg-slate-100"
                >
                  Sign in
                </Link>
              </>
            )}
          </div>
        </div>

        {/* Feature highlights */}
        <div className="grid gap-6 pt-8 sm:grid-cols-3">
          <div className="rounded-lg border border-slate-200 bg-white p-6 text-center">
            <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-brand/10">
              <svg
                className="h-6 w-6 text-brand"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                />
              </svg>
            </div>
            <h3 className="font-semibold text-slate-900">Secure OAuth</h3>
            <p className="mt-2 text-sm text-slate-600">
              PKCE-protected authentication flow
            </p>
          </div>

          <div className="rounded-lg border border-slate-200 bg-white p-6 text-center">
            <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-brand/10">
              <svg
                className="h-6 w-6 text-brand"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
            <h3 className="font-semibold text-slate-900">Email Verified</h3>
            <p className="mt-2 text-sm text-slate-600">
              Confirmation required before linking
            </p>
          </div>

          <div className="rounded-lg border border-slate-200 bg-white p-6 text-center">
            <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-brand/10">
              <svg
                className="h-6 w-6 text-brand"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z"
                />
              </svg>
            </div>
            <h3 className="font-semibold text-slate-900">Multi-Provider</h3>
            <p className="mt-2 text-sm text-slate-600">
              Google Drive & OneDrive support
            </p>
          </div>
        </div>
      </section>
    </div>
  );
};
