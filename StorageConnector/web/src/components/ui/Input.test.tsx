import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@/test/test-utils";
import userEvent from "@testing-library/user-event";
import { Input } from "./Input";
import { createRef } from "react";

describe("Input", () => {
  it("renders input element", () => {
    render(<Input />);
    expect(screen.getByRole("textbox")).toBeInTheDocument();
  });

  it("renders label when provided", () => {
    render(<Input label="Email" />);
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
  });

  it("does not render label when not provided", () => {
    const { container } = render(<Input />);
    const label = container.querySelector("label");
    expect(label).not.toBeInTheDocument();
  });

  it("associates label with input using htmlFor", () => {
    render(<Input label="Username" />);
    const label = screen.getByText(/username/i);
    const input = screen.getByLabelText(/username/i);
    expect(label).toHaveAttribute("for", input.id);
  });

  it("generates id from label", () => {
    render(<Input label="First Name" />);
    const input = screen.getByLabelText(/first name/i);
    expect(input).toHaveAttribute("id", "first-name");
  });

  it("uses custom id when provided", () => {
    render(<Input label="Email" id="custom-email-id" />);
    const input = screen.getByLabelText(/email/i);
    expect(input).toHaveAttribute("id", "custom-email-id");
  });

  it("renders error message when provided", () => {
    render(<Input label="Password" error="Password is required" />);
    expect(screen.getByRole("alert")).toHaveTextContent("Password is required");
  });

  it("does not render error when not provided", () => {
    render(<Input label="Email" />);
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("applies error styles when error is present", () => {
    render(<Input error="Error message" />);
    const input = screen.getByRole("textbox");
    expect(input).toHaveClass(
      "border-red-300",
      "focus:border-red-500",
      "focus:ring-red-500"
    );
  });

  it("applies base styles", () => {
    render(<Input />);
    const input = screen.getByRole("textbox");
    expect(input).toHaveClass(
      "block",
      "w-full",
      "rounded-md",
      "border",
      "border-slate-300",
      "px-3",
      "py-2",
      "text-sm"
    );
  });

  it("applies custom className", () => {
    render(<Input className="custom-input" />);
    const input = screen.getByRole("textbox");
    expect(input).toHaveClass("custom-input");
  });

  it("forwards ref to input element", () => {
    const ref = createRef<HTMLInputElement>();
    render(<Input ref={ref} />);
    expect(ref.current).toBeInstanceOf(HTMLInputElement);
  });

  it("handles user input", async () => {
    const user = userEvent.setup();
    render(<Input label="Email" />);
    const input = screen.getByLabelText(/email/i);

    await user.type(input, "test@example.com");

    expect(input).toHaveValue("test@example.com");
  });

  it("calls onChange handler", async () => {
    const user = userEvent.setup();
    const handleChange = vi.fn();
    render(<Input onChange={handleChange} />);
    const input = screen.getByRole("textbox");

    await user.type(input, "a");

    expect(handleChange).toHaveBeenCalled();
  });

  it("forwards HTML input attributes", () => {
    render(
      <Input
        type="email"
        placeholder="Enter email"
        disabled
        required
        maxLength={50}
      />
    );
    const input = screen.getByRole("textbox");

    expect(input).toHaveAttribute("type", "email");
    expect(input).toHaveAttribute("placeholder", "Enter email");
    expect(input).toBeDisabled();
    expect(input).toBeRequired();
    expect(input).toHaveAttribute("maxLength", "50");
  });

  it("applies disabled styles", () => {
    render(<Input disabled />);
    const input = screen.getByRole("textbox");
    expect(input).toHaveClass(
      "disabled:cursor-not-allowed",
      "disabled:bg-slate-50",
      "disabled:text-slate-500"
    );
  });

  it("renders password type input", () => {
    render(<Input type="password" label="Password" />);
    const input = screen.getByLabelText(/password/i);
    expect(input).toHaveAttribute("type", "password");
  });
});
