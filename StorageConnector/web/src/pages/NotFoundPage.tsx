import { Link, isRouteErrorResponse, useRouteError } from "react-router-dom";

export const NotFoundPage = () => {
  const error = useRouteError();
  const title =
    isRouteErrorResponse(error) && error.status === 404
      ? "Page not found"
      : "Something went wrong";

  return (
    <section className="mx-auto max-w-xl space-y-6 rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm">
      <h1 className="text-3xl font-bold text-slate-900">{title}</h1>
      <p className="text-sm text-slate-600">
        {isRouteErrorResponse(error)
          ? "We couldn’t find that page."
          : "An unexpected error occurred."}
      </p>
      <Link
        to="/"
        className="inline-flex items-center justify-center rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white hover:bg-brand-dark"
      >
        Go home
      </Link>
    </section>
  );
};
