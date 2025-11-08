import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { useAuthStore } from "../state/auth-store";
import { changePassword } from "../api/auth";
import { ApiError } from "../api/client";
import { Alert, Button, Card, Input, PageContainer, PageHeader, PageSection } from "../components/ui";

export const AccountPage = () => {
  const userEmail = useAuthStore((state) => state.userEmail);
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const changePasswordMutation = useMutation({
    mutationFn: changePassword,
    onSuccess: () => {
      setSuccessMessage("Password changed successfully!");
      setErrorMessage(null);
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
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

    if (newPassword !== confirmPassword) {
      setErrorMessage("New passwords do not match");
      return;
    }

    if (newPassword.length < 8) {
      setErrorMessage("New password must be at least 8 characters long");
      return;
    }

    changePasswordMutation.mutate({
      currentPassword,
      newPassword,
    });
  };

  return (
    <PageContainer>
      <PageSection>
        <PageHeader 
          title="Account Settings"
          description="Manage your account preferences and security settings"
        />
        <div className="grid gap-6 md:grid-cols-2">
          {/* Account Information */}
          <Card>
            <div className="p-6">
              <h2 className="text-lg font-semibold text-slate-900 mb-4">Account Information</h2>
              <div className="space-y-3">
                <div>
                  <label className="text-sm font-medium text-slate-700">Email</label>
                  <p className="text-sm text-slate-900 mt-1">{userEmail}</p>
                </div>
              </div>
            </div>
          </Card>
          {/* Change Password */}
          <Card>
            <div className="p-6">
              <h2 className="text-lg font-semibold text-slate-900 mb-4">Change Password</h2>
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
                  label="Current Password"
                  type="password"
                  value={currentPassword}
                  onChange={(e) => setCurrentPassword(e.target.value)}
                  required
                />
                <Input
                  label="New Password"
                  type="password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  required
                  minLength={8}
                />
                <Input
                  label="Confirm New Password"
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
                  isLoading={changePasswordMutation.isPending}
                >
                  Change Password
                </Button>
              </form>
            </div>
          </Card>
        </div>
      </PageSection>
    </PageContainer>
  );
};
