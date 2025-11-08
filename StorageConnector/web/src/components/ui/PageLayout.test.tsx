import { describe, it, expect } from "vitest";
import { render, screen } from "@/test/test-utils";
import { PageContainer, PageHeader, PageSection } from "./PageLayout";
import { Button } from "./Button";

describe("PageContainer", () => {
  it("renders children", () => {
    render(<PageContainer>Page content</PageContainer>);
    expect(screen.getByText(/page content/i)).toBeInTheDocument();
  });

  it("applies full width by default", () => {
    const { container } = render(<PageContainer>Content</PageContainer>);
    const div = container.firstChild as HTMLElement;
    expect(div).toHaveStyle({ maxWidth: "1600px" });
  });

  it("applies sm max width", () => {
    const { container } = render(
      <PageContainer maxWidth="sm">Content</PageContainer>
    );
    const div = container.firstChild as HTMLElement;
    expect(div).toHaveClass("max-w-md");
  });

  it("applies md max width", () => {
    const { container } = render(
      <PageContainer maxWidth="md">Content</PageContainer>
    );
    const div = container.firstChild as HTMLElement;
    expect(div).toHaveClass("max-w-2xl");
  });

  it("applies lg max width", () => {
    const { container } = render(
      <PageContainer maxWidth="lg">Content</PageContainer>
    );
    const div = container.firstChild as HTMLElement;
    expect(div).toHaveClass("max-w-4xl");
  });

  it("applies xl max width", () => {
    const { container } = render(
      <PageContainer maxWidth="xl">Content</PageContainer>
    );
    const div = container.firstChild as HTMLElement;
    expect(div).toHaveClass("max-w-7xl");
  });

  it("centers content with mx-auto", () => {
    const { container } = render(<PageContainer>Content</PageContainer>);
    const div = container.firstChild as HTMLElement;
    expect(div).toHaveClass("mx-auto");
  });
});

describe("PageHeader", () => {
  it("renders title", () => {
    render(<PageHeader title="Page Title" />);
    expect(
      screen.getByRole("heading", { name: /page title/i })
    ).toBeInTheDocument();
  });

  it("renders description when provided", () => {
    render(<PageHeader title="Title" description="This is a description" />);
    expect(screen.getByText(/this is a description/i)).toBeInTheDocument();
  });

  it("does not render description when not provided", () => {
    const { container } = render(<PageHeader title="Title" />);
    const description = container.querySelector("p");
    expect(description).not.toBeInTheDocument();
  });

  it("renders action element when provided", () => {
    render(
      <PageHeader title="Title" action={<Button>Action Button</Button>} />
    );
    expect(
      screen.getByRole("button", { name: /action button/i })
    ).toBeInTheDocument();
  });

  it("does not render action when not provided", () => {
    render(<PageHeader title="Title" />);
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("applies flex layout when action is provided", () => {
    const { container } = render(
      <PageHeader title="Title" action={<Button>Action</Button>} />
    );
    const header = container.querySelector("header");
    const flexDiv = header?.querySelector("div");
    expect(flexDiv).toHaveClass("flex", "items-center", "justify-between");
  });

  it("applies header element", () => {
    const { container } = render(<PageHeader title="Title" />);
    expect(container.querySelector("header")).toBeInTheDocument();
  });

  it("applies correct heading styles", () => {
    render(<PageHeader title="Styled Title" />);
    const heading = screen.getByRole("heading");
    expect(heading).toHaveClass("text-3xl", "font-bold", "text-slate-900");
  });

  it("applies correct description styles", () => {
    render(<PageHeader title="Title" description="Description text" />);
    const description = screen.getByText(/description text/i);
    expect(description).toHaveClass("text-sm", "text-slate-600", "mt-2");
  });
});

describe("PageSection", () => {
  it("renders children", () => {
    render(<PageSection>Section content</PageSection>);
    expect(screen.getByText(/section content/i)).toBeInTheDocument();
  });

  it("applies section element", () => {
    const { container } = render(<PageSection>Content</PageSection>);
    expect(container.querySelector("section")).toBeInTheDocument();
  });

  it("applies base spacing styles", () => {
    const { container } = render(<PageSection>Content</PageSection>);
    const section = container.querySelector("section");
    expect(section).toHaveClass("space-y-8");
  });

  it("applies custom className", () => {
    const { container } = render(
      <PageSection className="custom-section">Content</PageSection>
    );
    const section = container.querySelector("section");
    expect(section).toHaveClass("custom-section", "space-y-8");
  });

  it("renders complex children", () => {
    render(
      <PageSection>
        <div data-testid="child-1">Child 1</div>
        <div data-testid="child-2">Child 2</div>
      </PageSection>
    );

    expect(screen.getByTestId("child-1")).toBeInTheDocument();
    expect(screen.getByTestId("child-2")).toBeInTheDocument();
  });
});
