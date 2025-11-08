import type { ReactNode } from "react";

type AlertVariant = "success" | "error" | "info" | "warning";

interface AlertProps {
  variant: AlertVariant;
  children: ReactNode;
  className?: string;
}

const variantStyles: Record<AlertVariant, string> = {
  success: "border-emerald-200 bg-emerald-50 text-emerald-700",
  error: "border-red-200 bg-red-50 text-red-600",
  info: "border-blue-200 bg-blue-50 text-blue-700",
  warning: "border-amber-200 bg-amber-50 text-amber-700",
};

export const Alert = ({ variant, children, className = "" }: AlertProps) => {
  return (
    <div
      className={`rounded-md border px-4 py-3 text-sm ${variantStyles[variant]} ${className}`}
      role="alert"
    >
      {children}
    </div>
  );
};
