import { forwardRef, useState } from "react";
import type { InputHTMLAttributes } from "react";

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, className = "", id, type = "text", ...props }, ref) => {
    const inputId = id || label?.toLowerCase().replace(/\s+/g, "-");
    const [isPasswordVisible, setIsPasswordVisible] = useState(false);
    const isPassword = type === "password";
    const inputType = isPassword && isPasswordVisible ? "text" : type;

    return (
      <div className="space-y-2">
        {label && (
          <label
            htmlFor={inputId}
            className="block text-sm font-medium text-slate-700"
          >
            {label}
          </label>
        )}
        <div className="relative">
          <input
            ref={ref}
            id={inputId}
            type={inputType}
            className={`
              block w-full rounded-md border border-slate-300 px-3 py-2 text-sm
              focus:border-brand focus:outline-none focus:ring-1 focus:ring-brand
              disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-500
              ${error ? "border-red-300 focus:border-red-500 focus:ring-red-500" : ""}
              ${isPassword ? "pr-12" : ""}
              ${className}
            `}
            {...props}
          />
          {isPassword && (
            <button
              type="button"
              onClick={() => setIsPasswordVisible((prev) => !prev)}
              className="absolute inset-y-0 right-0 flex items-center pr-3 text-xs font-semibold text-slate-500 hover:text-slate-700 focus:outline-none"
              aria-label={`${isPasswordVisible ? "Hide" : "Show"} password`}
              aria-pressed={isPasswordVisible}
              disabled={props.disabled}
            >
              {isPasswordVisible ? "Hide" : "Show"}
            </button>
          )}
        </div>
        {error && (
          <p className="text-xs text-red-600" role="alert">
            {error}
          </p>
        )}
      </div>
    );
  }
);

Input.displayName = "Input";
