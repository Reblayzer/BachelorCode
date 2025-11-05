import { Link } from "react-router-dom";

export const EmailConfirmedPage = () => {
  return (
    <div className="mx-auto max-w-md">
      <div className="rounded-2xl border border-slate-200 bg-white p-8 shadow-sm text-center">
        <div className="mb-6">
          <div className="mx-auto w-16 h-16 bg-emerald-100 rounded-full flex items-center justify-center mb-4">
            <svg className="w-8 h-8 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
            </svg>
          </div>
          <h1 className="text-2xl font-bold text-slate-900 mb-2">Email Confirmed!</h1>
          <p className="text-sm text-slate-600">
            Your email address has been successfully verified. You can now sign in to your account.
          </p>
        </div>

        <Link
          to="/login"
          className="inline-block w-full rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white transition hover:bg-brand-dark"
        >
          Continue to Login
        </Link>
      </div>
    </div>
  );
};
