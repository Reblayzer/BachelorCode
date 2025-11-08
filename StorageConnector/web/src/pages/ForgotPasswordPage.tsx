import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { Link } from "react-router-dom";
import { requestPasswordReset } from "../api/auth";
import { ApiError } from "../api/client";
import { Alert, Button, Card, CardHeader, Input, PageContainer } from "../components/ui";

export const ForgotPasswordPage = () => {
  const [email, setEmail] = useState("");
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const requestResetMutation = useMutation({
    mutationFn: requestPasswordReset,
    onSuccess: () => {
      setSuccessMessage("Password reset link has been sent to your email address.");
      setErrorMessage(null);
      setEmail("");
    },
    onError: (error: unknown) => {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("An unexpected error occurred");
      }
      setSuccessMessage(null);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSuccessMessage(null);
    setErrorMessage(null);
    requestResetMutation.mutate({ email });
  };

  return (
    <PageContainer maxWidth="sm">
      <Card className="p-8">
        <CardHeader
          title="Forgot Password"
          description="Enter your email address and we'll send you a link to reset your password."
        />
        {successMessage && (
          <Alert variant="success" className="mb-4">
            {successMessage}
          </Alert>
        )}
        {errorMessage && (
          <Alert variant="error" className="mb-4">
            {errorMessage}
          </Alert>
        )}
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            type="email"
            label="Email Address"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
          <Button
            type="submit"
            isLoading={requestResetMutation.isPending}
            fullWidth
          >
            Send Reset Link
          </Button>
        </form>
        <div className="mt-6 text-center text-sm">
          <Link to="/login" className="font-medium text-brand hover:text-brand-dark">
            Back to Login
          </Link>
        </div>
      </Card>
    </PageContainer>
  );
};
