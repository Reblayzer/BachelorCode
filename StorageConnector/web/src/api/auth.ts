import {
  identityRequest,
} from "./client";
import type {
  CurrentUserResponse,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
} from "./types";

export const register = (payload: RegisterRequest) =>
  identityRequest<RegisterResponse>("/api/auth/register", {
    method: "POST",
    body: payload,
  });

export const login = (payload: LoginRequest) =>
  identityRequest<LoginResponse>("/api/auth/login", {
    method: "POST",
    body: payload,
  });

export const logout = () =>
  identityRequest<void>("/api/auth/logout", {
    method: "POST",
  });

export const resendConfirmation = (payload: { email: string }) =>
  identityRequest<RegisterResponse>("/api/auth/resend-confirmation", {
    method: "POST",
    body: payload,
  });

export const getCurrentUser = () =>
  identityRequest<CurrentUserResponse>("/api/auth/me", {
    method: "GET",
  });

export const changePassword = (payload: { currentPassword: string; newPassword: string }) =>
  identityRequest<void>("/api/auth/change-password", {
    method: "POST",
    body: payload,
  });

export const requestPasswordReset = (payload: { email: string }) =>
  identityRequest<void>("/api/auth/forgot-password", {
    method: "POST",
    body: payload,
  });

export const resetPassword = (payload: { email: string; token: string; newPassword: string }) =>
  identityRequest<void>("/api/auth/reset-password", {
    method: "POST",
    body: payload,
  });
