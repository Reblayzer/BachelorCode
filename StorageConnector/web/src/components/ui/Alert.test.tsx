import { describe, it, expect } from "vitest";
import { render, screen } from "@/test/test-utils";
import { Alert } from "./Alert";

describe("Alert", () => {
  it("renders children", () => {
    render(<Alert variant="info">Alert message</Alert>);
    expect(screen.getByRole("alert")).toHaveTextContent("Alert message");
  });

  it('has role="alert" for accessibility', () => {
    render(<Alert variant="info">Test</Alert>);
    expect(screen.getByRole("alert")).toBeInTheDocument();
  });

  it("applies success variant styles", () => {
    render(<Alert variant="success">Success message</Alert>);
    const alert = screen.getByRole("alert");
    expect(alert).toHaveClass(
      "border-emerald-200",
      "bg-emerald-50",
      "text-emerald-700"
    );
  });

  it("applies error variant styles", () => {
    render(<Alert variant="error">Error message</Alert>);
    const alert = screen.getByRole("alert");
    expect(alert).toHaveClass("border-red-200", "bg-red-50", "text-red-600");
  });

  it("applies info variant styles", () => {
    render(<Alert variant="info">Info message</Alert>);
    const alert = screen.getByRole("alert");
    expect(alert).toHaveClass("border-blue-200", "bg-blue-50", "text-blue-700");
  });

  it("applies warning variant styles", () => {
    render(<Alert variant="warning">Warning message</Alert>);
    const alert = screen.getByRole("alert");
    expect(alert).toHaveClass(
      "border-amber-200",
      "bg-amber-50",
      "text-amber-700"
    );
  });

  it("applies base styles", () => {
    render(<Alert variant="info">Test</Alert>);
    const alert = screen.getByRole("alert");
    expect(alert).toHaveClass(
      "rounded-md",
      "border",
      "px-4",
      "py-3",
      "text-sm"
    );
  });

  it("applies custom className", () => {
    render(
      <Alert variant="info" className="custom-alert">
        Test
      </Alert>
    );
    const alert = screen.getByRole("alert");
    expect(alert).toHaveClass("custom-alert");
  });

  it("renders complex children", () => {
    render(
      <Alert variant="info">
        <strong>Important:</strong> This is a test message.
      </Alert>
    );

    expect(screen.getByText(/important/i)).toBeInTheDocument();
    expect(screen.getByText(/this is a test message/i)).toBeInTheDocument();
  });
});
