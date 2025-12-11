import { useMutation } from "@tanstack/react-query";
import { type FormEvent, useState } from "react";
import { Link } from "react-router-dom";
import { register } from "../api/auth";
import { ApiError } from "../api/client";
import { Alert, Button, Card, Input, PageContainer } from "../components/ui";

export const RegisterPage = () => {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const registerMutation = useMutation({
    mutationFn: register,
    onSuccess: (data) => {
      setSuccessMessage(
        data.message ??
          "Registration successful. Check your inbox to confirm your email."
      );
      setErrorMessage(null);
      setPassword("");
      setConfirm("");
    },
    onError: (error: unknown) => {
      if (error instanceof ApiError) {
        // Backend returns detailed validation errors
        if (error.data && Array.isArray(error.data)) {
          // Multiple validation errors (e.g., from Identity)
          setErrorMessage(error.data.join(" "));
        } else if (error.message) {
          setErrorMessage(error.message);
        } else {
          setErrorMessage(
            "Unable to create account. Please check your information."
          );
        }
        return;
      }
      setErrorMessage("Unexpected error while creating your account.");
    },
  });

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setErrorMessage(null);
    setSuccessMessage(null);

    // Client-side validation
    if (password !== confirm) {
      setErrorMessage("Passwords do not match. Please try again.");
      return;
    }

    if (password.length < 8) {
      setErrorMessage("Password must be at least 8 characters long.");
      return;
    }

    registerMutation.mutate({ email, password });
  };

  return (
    <PageContainer maxWidth="sm">
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-slate-900">
            Create your account
          </h1>
          <p className="mt-2 text-sm text-slate-600">
            Already have an account?{" "}
            <Link to="/login" className="font-semibold text-brand">
              Sign in
            </Link>
          </p>
        </div>
        <Card>
          <form onSubmit={handleSubmit} className="space-y-5 p-6">
            {successMessage && (
              <Alert variant="success">{successMessage}</Alert>
            )}
            {errorMessage && <Alert variant="error">{errorMessage}</Alert>}
            <Input
              label="Email"
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required
            />
            <Input
              label="Password"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
              minLength={8}
            />
            <Input
              label="Confirm password"
              type="password"
              value={confirm}
              onChange={(event) => setConfirm(event.target.value)}
              required
              minLength={8}
            />
            <Button
              type="submit"
              variant="primary"
              fullWidth
              isLoading={registerMutation.isPending}
            >
              Create account
            </Button>
          </form>
        </Card>
      </div>
    </PageContainer>
  );
};
