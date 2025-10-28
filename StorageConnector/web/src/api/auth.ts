import {
  identityRequest,
} from "./client";
import type {
  CurrentUserResponse,
  LoginRequest,
  RegisterRequest,
  RegisterResponse,
} from "./types";

export const register = (payload: RegisterRequest) =>
  identityRequest<RegisterResponse>("/api/auth/register", {
    method: "POST",
    body: payload,
  });

export const login = (payload: LoginRequest) =>
  identityRequest<void>("/api/auth/login", {
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
