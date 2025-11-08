import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from './auth-store';

describe('auth-store', () => {
  beforeEach(() => {
    // Reset store to initial state
    useAuthStore.setState({
      isAuthenticated: false,
      userEmail: undefined,
    });
  });

  it('initializes with unauthenticated state', () => {
    const state = useAuthStore.getState();

    expect(state.isAuthenticated).toBe(false);
    expect(state.userEmail).toBeUndefined();
  });

  it('sets authenticated with email', () => {
    const { setAuthenticated } = useAuthStore.getState();

    setAuthenticated('test@example.com');

    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.userEmail).toBe('test@example.com');
  });

  it('sets authenticated without email parameter', () => {
    // First set an email
    useAuthStore.setState({
      isAuthenticated: true,
      userEmail: 'existing@example.com',
    });

    const { setAuthenticated } = useAuthStore.getState();

    // Call setAuthenticated without email - should preserve existing email
    setAuthenticated();

    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.userEmail).toBe('existing@example.com');
  });

  it('sets authenticated with undefined explicitly preserves existing email', () => {
    // First set an email
    useAuthStore.setState({
      isAuthenticated: true,
      userEmail: 'existing@example.com',
    });

    const { setAuthenticated } = useAuthStore.getState();

    // Call setAuthenticated with undefined - should preserve existing email
    setAuthenticated(undefined);

    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.userEmail).toBe('existing@example.com');
  });

  it('clears authentication state', () => {
    // Set authenticated state first
    useAuthStore.setState({
      isAuthenticated: true,
      userEmail: 'test@example.com',
    });

    const { clear } = useAuthStore.getState();
    clear();

    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.userEmail).toBeUndefined();
  });

  it('can update email when already authenticated', () => {
    const { setAuthenticated } = useAuthStore.getState();

    setAuthenticated('first@example.com');
    expect(useAuthStore.getState().userEmail).toBe('first@example.com');

    setAuthenticated('second@example.com');
    expect(useAuthStore.getState().userEmail).toBe('second@example.com');
    expect(useAuthStore.getState().isAuthenticated).toBe(true);
  });
});
