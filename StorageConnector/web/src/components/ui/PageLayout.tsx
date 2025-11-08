import type { ReactNode } from "react";

interface PageContainerProps {
  children: ReactNode;
  maxWidth?: "sm" | "md" | "lg" | "xl" | "full";
}

const maxWidthStyles = {
  sm: "max-w-md",
  md: "max-w-2xl",
  lg: "max-w-4xl",
  xl: "max-w-7xl",
  full: "",
};

export const PageContainer = ({ children, maxWidth = "full" }: PageContainerProps) => {
  const widthClass = maxWidth === "full" ? "" : maxWidthStyles[maxWidth];
  const style = maxWidth === "full" ? { maxWidth: "1600px" } : undefined;

  return (
    <div className={`mx-auto ${widthClass}`} style={style}>
      {children}
    </div>
  );
};

interface PageHeaderProps {
  title: string;
  description?: string;
  action?: ReactNode;
}

export const PageHeader = ({ title, description, action }: PageHeaderProps) => {
  return (
    <header className="space-y-4">
      <div className={action ? "flex items-center justify-between" : ""}>
        <div>
          <h1 className="text-3xl font-bold text-slate-900">{title}</h1>
          {description && (
            <p className="text-sm text-slate-600 mt-2">{description}</p>
          )}
        </div>
        {action && <div>{action}</div>}
      </div>
    </header>
  );
};

interface PageSectionProps {
  children: ReactNode;
  className?: string;
}

export const PageSection = ({ children, className = "" }: PageSectionProps) => {
  return <section className={`space-y-8 ${className}`}>{children}</section>;
};
