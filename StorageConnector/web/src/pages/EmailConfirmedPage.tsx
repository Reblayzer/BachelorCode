import { Link } from "react-router-dom";
import { Button, Card, CardHeader, PageContainer } from "../components/ui";

export const EmailConfirmedPage = () => {
  return (
    <PageContainer maxWidth="sm">
      <Card className="p-8 text-center">
        <div className="mx-auto w-16 h-16 bg-emerald-100 rounded-full flex items-center justify-center mb-6">
          <svg
            className="w-8 h-8 text-emerald-600"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M5 13l4 4L19 7"
            />
          </svg>
        </div>
        <CardHeader
          title="Email Confirmed!"
          description="Your email address has been successfully verified. You can now sign in to your account."
        />
        <Link to="/login">
          <Button variant="primary" fullWidth>
            Continue to Login
          </Button>
        </Link>
      </Card>
    </PageContainer>
  );
};
