import { createBrowserRouter } from "react-router-dom";
import { AppLayout } from "../components/AppLayout";
import { LandingPage } from "../pages/LandingPage";
import { RegisterPage } from "../pages/RegisterPage";
import { LoginPage } from "../pages/LoginPage";
import { ConnectionsPage } from "../pages/ConnectionsPage";
import { FilesPage } from "../pages/FilesPage";
import { AccountPage } from "../pages/AccountPage";
import { EmailConfirmedPage } from "../pages/EmailConfirmedPage";
import { ForgotPasswordPage } from "../pages/ForgotPasswordPage";
import { ResetPasswordPage } from "../pages/ResetPasswordPage";
import { LinkResultPage } from "../pages/LinkResultPage";
import { NotFoundPage } from "../pages/NotFoundPage";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <AppLayout />,
    errorElement: <NotFoundPage />,
    children: [
      { index: true, element: <LandingPage /> },
      { path: "register", element: <RegisterPage /> },
      { path: "login", element: <LoginPage /> },
      { path: "connections", element: <ConnectionsPage /> },
      { path: "files", element: <FilesPage /> },
      { path: "account", element: <AccountPage /> },
      { path: "auth/confirmed", element: <EmailConfirmedPage /> },
      { path: "forgot-password", element: <ForgotPasswordPage /> },
      { path: "reset-password", element: <ResetPasswordPage /> },
      {
        path: "connections/success",
        element: <LinkResultPage variant="success" />,
      },
      {
        path: "connections/error",
        element: <LinkResultPage variant="error" />,
      },
      { path: "*", element: <NotFoundPage /> },
    ],
  },
]);
