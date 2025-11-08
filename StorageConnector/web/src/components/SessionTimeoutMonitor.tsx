import { useEffect, useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "../state/auth-store";
import { getCurrentUser } from "../api/auth";
import { Button, Card } from "./ui";

const SESSION_TIMEOUT = 60 * 60 * 1000; // 1 hour in milliseconds
const WARNING_TIME = 10 * 1000; // 10 seconds before timeout
const COUNTDOWN_DURATION = 10; // 10 seconds countdown

export const SessionTimeoutMonitor = () => {
  const navigate = useNavigate();
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const clear = useAuthStore((state) => state.clear);

  const [showWarning, setShowWarning] = useState(false);
  const [countdown, setCountdown] = useState(COUNTDOWN_DURATION);
  const [lastActivity, setLastActivity] = useState(Date.now());

  // Reset activity timer on user interaction
  const resetActivity = useCallback(() => {
    setLastActivity(Date.now());
    setShowWarning(false);
    setCountdown(COUNTDOWN_DURATION);
  }, []);

  // Extend session by making an API call
  const extendSession = useCallback(async () => {
    try {
      await getCurrentUser(); // This will refresh the session cookie
      resetActivity();
    } catch (error) {
      // If session is already expired, log out
      handleTimeout();
    }
  }, [resetActivity]);

  // Handle session timeout
  const handleTimeout = useCallback(() => {
    clear();
    setShowWarning(false);
    navigate("/login", {
      state: { message: "Your session has expired. Please sign in again." },
    });
  }, [clear, navigate]);

  // Monitor user activity
  useEffect(() => {
    if (!isAuthenticated) return;

    const activityEvents = ["mousedown", "keydown", "scroll", "touchstart"];

    activityEvents.forEach((event) => {
      window.addEventListener(event, resetActivity);
    });

    return () => {
      activityEvents.forEach((event) => {
        window.removeEventListener(event, resetActivity);
      });
    };
  }, [isAuthenticated, resetActivity]);

  // Check session timeout
  useEffect(() => {
    if (!isAuthenticated) return;

    const checkInterval = setInterval(() => {
      const now = Date.now();
      const timeSinceLastActivity = now - lastActivity;
      const timeUntilTimeout = SESSION_TIMEOUT - timeSinceLastActivity;

      // Show warning 10 seconds before timeout
      if (timeUntilTimeout <= WARNING_TIME && timeUntilTimeout > 0) {
        setShowWarning(true);
      }

      // Session expired
      if (timeSinceLastActivity >= SESSION_TIMEOUT) {
        handleTimeout();
      }
    }, 1000); // Check every second

    return () => clearInterval(checkInterval);
  }, [isAuthenticated, lastActivity, handleTimeout]);

  // Countdown timer when warning is shown
  useEffect(() => {
    if (!showWarning) {
      setCountdown(COUNTDOWN_DURATION);
      return;
    }

    const countdownInterval = setInterval(() => {
      setCountdown((prev) => {
        if (prev <= 1) {
          handleTimeout();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(countdownInterval);
  }, [showWarning, handleTimeout]);

  if (!showWarning || !isAuthenticated) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <Card className="max-w-md p-6 shadow-xl">
        <div className="text-center space-y-4">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-amber-100">
            <svg
              className="h-6 w-6 text-amber-600"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
          </div>

          <div>
            <h2 className="text-xl font-bold text-slate-900">
              Session Expiring
            </h2>
            <p className="mt-2 text-sm text-slate-600">
              Your session is about to expire due to inactivity. Would you like
              to extend your session?
            </p>
          </div>

          <div className="text-center">
            <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-brand/10 mb-2">
              <span className="text-3xl font-bold text-brand">{countdown}</span>
            </div>
            <p className="text-xs text-slate-500">seconds remaining</p>
          </div>

          <div className="flex gap-3">
            <Button variant="secondary" fullWidth onClick={handleTimeout}>
              Sign Out
            </Button>
            <Button variant="primary" fullWidth onClick={extendSession}>
              Stay Signed In
            </Button>
          </div>
        </div>
      </Card>
    </div>
  );
};
