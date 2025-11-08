import { describe, it, expect } from "vitest";
import { render, screen } from "@/test/test-utils";
import { Card, CardHeader, CardContent } from "./Card";

describe("Card", () => {
  it("renders children", () => {
    render(<Card>Card content</Card>);
    expect(screen.getByText(/card content/i)).toBeInTheDocument();
  });

  it("applies base styles", () => {
    const { container } = render(<Card>Content</Card>);
    const card = container.firstChild as HTMLElement;
    expect(card).toHaveClass(
      "rounded-2xl",
      "border",
      "border-slate-200",
      "bg-white",
      "p-6",
      "shadow-sm"
    );
  });

  it("applies custom className", () => {
    const { container } = render(<Card className="custom-class">Content</Card>);
    const card = container.firstChild as HTMLElement;
    expect(card).toHaveClass("custom-class");
  });

  it("composes with CardHeader and CardContent", () => {
    render(
      <Card>
        <CardHeader title="Test Title" description="Test Description" />
        <CardContent>Test Content</CardContent>
      </Card>
    );

    expect(screen.getByText(/test title/i)).toBeInTheDocument();
    expect(screen.getByText(/test description/i)).toBeInTheDocument();
    expect(screen.getByText(/test content/i)).toBeInTheDocument();
  });
});

describe("CardHeader", () => {
  it("renders title", () => {
    render(<CardHeader title="Card Title" />);
    expect(
      screen.getByRole("heading", { name: /card title/i })
    ).toBeInTheDocument();
  });

  it("renders description when provided", () => {
    render(<CardHeader title="Title" description="This is a description" />);
    expect(screen.getByText(/this is a description/i)).toBeInTheDocument();
  });

  it("does not render description when not provided", () => {
    const { container } = render(<CardHeader title="Title" />);
    const paragraph = container.querySelector("p");
    expect(paragraph).not.toBeInTheDocument();
  });

  it("applies correct heading styles", () => {
    render(<CardHeader title="Styled Title" />);
    const heading = screen.getByRole("heading");
    expect(heading).toHaveClass("text-lg", "font-semibold", "text-slate-900");
  });

  it("applies correct description styles", () => {
    render(<CardHeader title="Title" description="Description" />);
    const description = screen.getByText(/description/i);
    expect(description).toHaveClass("text-sm", "text-slate-600", "mt-1");
  });
});

describe("CardContent", () => {
  it("renders children", () => {
    render(<CardContent>Content text</CardContent>);
    expect(screen.getByText(/content text/i)).toBeInTheDocument();
  });

  it("applies custom className", () => {
    const { container } = render(
      <CardContent className="custom-content">Content</CardContent>
    );
    const content = container.firstChild as HTMLElement;
    expect(content).toHaveClass("custom-content");
  });

  it("renders complex children", () => {
    render(
      <CardContent>
        <div data-testid="complex-child">
          <p>Paragraph</p>
          <span>Span</span>
        </div>
      </CardContent>
    );

    expect(screen.getByTestId("complex-child")).toBeInTheDocument();
    expect(screen.getByText(/paragraph/i)).toBeInTheDocument();
    expect(screen.getByText(/span/i)).toBeInTheDocument();
  });
});
