import { useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { Link, useSearchParams } from "react-router-dom";

type LinkResultPageProps = {
  variant: "success" | "error";
};

export const LinkResultPage = ({ variant }: LinkResultPageProps) => {
  const [searchParams] = useSearchParams();
  const provider = searchParams.get("provider");
  const error = searchParams.get("error");
  const queryClient = useQueryClient();

  const isSuccess = variant === "success";

  useEffect(() => {
    if (isSuccess) {
      // Just invalidate queries - token is already in localStorage
      queryClient.invalidateQueries({ queryKey: ["connections"] });
    }
  }, [isSuccess, queryClient]);

  return (
    <section className="mx-auto max-w-xl space-y-6 rounded-2xl border border-slate-200 bg-white p-8 text-center shadow-sm">
      <div
        className={`inline-flex h-12 w-12 items-center justify-center rounded-full ${
          isSuccess
            ? "bg-emerald-100 text-emerald-600"
            : "bg-red-100 text-red-600"
        }`}
      >
        {isSuccess ? "✓" : "!"}
      </div>
      <div className="space-y-2">
        <h1 className="text-2xl font-semibold text-slate-900">
          {isSuccess ? "Connection successful" : "We couldn’t finish linking"}
        </h1>
        <p className="text-sm text-slate-600">
          {isSuccess
            ? `Your ${provider ?? "storage"} account is now linked.`
            : (error ??
              "The link could not be completed. Please try again or start a fresh link from the connections page.")}
        </p>
      </div>
      <div className="flex justify-center gap-3">
        <Link
          to="/connections"
          className="rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white hover:bg-brand-dark"
        >
          Back to connections
        </Link>
        <Link
          to="/"
          className="rounded-md border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-100"
        >
          Home
        </Link>
      </div>
    </section>
  );
};
