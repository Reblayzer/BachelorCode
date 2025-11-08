import { vi } from 'vitest';

export const login = vi.fn();
export const register = vi.fn();
export const getCurrentUser = vi.fn();
export const logout = vi.fn();
export const changePassword = vi.fn();
export const requestPasswordReset = vi.fn();
export const resetPassword = vi.fn();
export const resendConfirmation = vi.fn();
