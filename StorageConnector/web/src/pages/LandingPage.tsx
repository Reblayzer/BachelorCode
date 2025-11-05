  import { Link } from "react-router-dom";
import { useAuthStore } from "../state/auth-store";

export const LandingPage = () => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  return (
    <div className="mx-auto" style={{ maxWidth: '1600px' }}>
      <section className="grid gap-10 md:grid-cols-[1.5fr_1fr]">
      <div className="space-y-6">
        <h1 className="text-4xl font-bold tracking-tight text-slate-900 sm:text-5xl">
          Manage your cloud storage links in one place.
        </h1>
        <p className="max-w-xl text-lg text-slate-600">
          Link Google and Microsoft storage accounts securely, see what scopes
          you have granted, and disconnect them with a single click.
        </p>
        <div className="flex flex-wrap gap-3">
          {isAuthenticated ? (
            <Link
              to="/connections"
              className="rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white shadow hover:bg-brand-dark"
            >
              View connections
            </Link>
          ) : (
            <>
              <Link
                to="/register"
                className="rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white shadow hover:bg-brand-dark"
              >
                Create account
              </Link>
              <Link
                to="/login"
                className="rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
              >
                Sign in
              </Link>
            </>
          )}
        </div>
      </div>
      <div className="rounded-2xl border border-dashed border-brand/30 bg-white p-6 shadow-sm">
        <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-brand-dark">
          Today’s status
        </h2>
        <ul className="space-y-4 text-sm text-slate-600">
          <li>✓ OAuth flows use PKCE and encrypted refresh tokens.</li>
          <li>✓ Email verification is enforced before linking.</li>
          <li>
            {isAuthenticated
              ? "You are signed in and can link providers."
              : "Sign in to start linking providers."}
          </li>
        </ul>
      </div>
    </section>
    </div>
  );
};
