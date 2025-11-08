import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { resetPassword } from "../api/auth";
import { ApiError } from "../api/client";
import {
  Alert,
  Button,
  Card,
  CardHeader,
  Input,
  PageContainer,
} from "../components/ui";

export const ResetPasswordPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get("token");
  const email = searchParams.get("email");

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const resetPasswordMutation = useMutation({
    mutationFn: resetPassword,
    onSuccess: () => {
      navigate("/login", {
        state: {
          message:
            "Password reset successfully! You can now log in with your new password.",
        },
      });
    },
    onError: (error: unknown) => {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("An unexpected error occurred");
      }
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);

    if (!token || !email) {
      setErrorMessage(
        "Invalid reset link. Please request a new password reset."
      );
      return;
    }

    if (newPassword !== confirmPassword) {
      setErrorMessage("Passwords do not match");
      return;
    }

    if (newPassword.length < 8) {
      setErrorMessage("Password must be at least 8 characters long");
      return;
    }

    resetPasswordMutation.mutate({
      email,
      token,
      newPassword,
    });
  };

  if (!token || !email) {
    return (
      <PageContainer maxWidth="sm">
        <Card className="p-8 text-center">
          <CardHeader
            title="Invalid Reset Link"
            description="This password reset link is invalid or has expired."
          />
          <Link to="/forgot-password">
            <Button variant="primary" fullWidth>
              Request New Reset Link
            </Button>
          </Link>
        </Card>
      </PageContainer>
    );
  }

  return (
    <PageContainer maxWidth="sm">
      <Card className="p-8">
        <CardHeader
          title="Reset Password"
          description="Enter your new password below."
        />
        {errorMessage && (
          <Alert variant="error" className="mb-4">
            {errorMessage}
          </Alert>
        )}
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="New Password"
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
            minLength={8}
          />
          <Input
            label="Confirm Password"
            type="password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            required
            minLength={8}
          />
          <Button
            type="submit"
            variant="primary"
            fullWidth
            isLoading={resetPasswordMutation.isPending}
          >
            Reset Password
          </Button>
        </form>
        <div className="mt-6 text-center text-sm">
          <Link
            to="/login"
            className="font-medium text-brand hover:text-brand-dark"
          >
            Back to Login
          </Link>
        </div>
      </Card>
    </PageContainer>
  );
};
