import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { ApiError, identityRequest, linkingRequest } from './client';

describe('ApiError', () => {
  it('creates error with message and status', () => {
    const error = new ApiError('Test error', 404);

    expect(error.message).toBe('Test error');
    expect(error.status).toBe(404);
    expect(error.name).toBe('ApiError');
    expect(error.data).toBeUndefined();
  });

  it('creates error with data payload', () => {
    const data = { field: 'value' };
    const error = new ApiError('Test error', 400, data);

    expect(error.message).toBe('Test error');
    expect(error.status).toBe(400);
    expect(error.data).toEqual(data);
  });
});

describe('identityRequest', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    global.fetch = fetchMock as unknown as typeof fetch;
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('makes GET request with default headers', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ success: true }),
    });

    const result = await identityRequest<{ success: boolean }>('/test');

    expect(fetchMock).toHaveBeenCalledWith(
      '/test',
      expect.objectContaining({
        headers: expect.objectContaining({
          Accept: 'application/json',
        }),
        credentials: 'include',
      })
    );
    expect(result).toEqual({ success: true });
  });

  it('sends JSON body with POST request', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ id: 123 }),
    });

    await identityRequest('/users', {
      method: 'POST',
      body: { name: 'Test User' },
    });

    expect(fetchMock).toHaveBeenCalledWith(
      '/users',
      expect.objectContaining({
        method: 'POST',
        headers: expect.objectContaining({
          'Content-Type': 'application/json',
        }),
        body: JSON.stringify({ name: 'Test User' }),
      })
    );
  });

  it('sends FormData body without Content-Type header', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ success: true }),
    });

    const formData = new FormData();
    formData.append('file', 'test');

    await identityRequest('/upload', {
      method: 'POST',
      body: formData,
    });

    const call = fetchMock.mock.calls[0];
    expect(call[1].body).toBeInstanceOf(FormData);
    expect(call[1].headers['Content-Type']).toBeUndefined();
  });

  it('handles 204 No Content response', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 204,
      headers: new Headers(),
    });

    const result = await identityRequest('/delete');

    expect(result).toBeUndefined();
  });

  it('handles 205 Reset Content response', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 205,
      headers: new Headers(),
    });

    const result = await identityRequest('/reset');

    expect(result).toBeUndefined();
  });

  it('handles non-JSON response', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'text/plain' }),
      text: async () => 'Plain text response',
    });

    const result = await identityRequest('/text');

    expect(result).toBe('Plain text response');
  });

  it('handles empty text response', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'text/plain' }),
      text: async () => '',
    });

    const result = await identityRequest('/empty');

    expect(result).toBeUndefined();
  });

  it('throws ApiError on 404 with JSON error', async () => {
    fetchMock.mockResolvedValue({
      ok: false,
      status: 404,
      statusText: 'Not Found',
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ message: 'Resource not found' }),
    });

    await expect(identityRequest('/missing')).rejects.toThrow(ApiError);

    try {
      await identityRequest('/missing');
    } catch (error) {
      expect(error).toBeInstanceOf(ApiError);
      expect((error as ApiError).status).toBe(404);
      expect((error as ApiError).message).toBe('Resource not found');
      expect((error as ApiError).data).toEqual({ message: 'Resource not found' });
    }
  });

  it('throws ApiError with statusText when no message in payload', async () => {
    fetchMock.mockResolvedValue({
      ok: false,
      status: 500,
      statusText: 'Internal Server Error',
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({}),
    });

    try {
      await identityRequest('/error');
    } catch (error) {
      expect(error).toBeInstanceOf(ApiError);
      expect((error as ApiError).message).toBe('Internal Server Error');
    }
  });

  it('throws ApiError with default message when statusText is empty', async () => {
    fetchMock.mockResolvedValue({
      ok: false,
      status: 400,
      statusText: '',
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({}),
    });

    try {
      await identityRequest('/bad');
    } catch (error) {
      expect(error).toBeInstanceOf(ApiError);
      expect((error as ApiError).message).toBe('Request failed');
    }
  });

  it('handles malformed JSON in error response', async () => {
    fetchMock.mockResolvedValue({
      ok: false,
      status: 500,
      statusText: 'Server Error',
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => {
        throw new Error('Invalid JSON');
      },
    });

    try {
      await identityRequest('/malformed');
    } catch (error) {
      expect(error).toBeInstanceOf(ApiError);
      expect((error as ApiError).message).toBe('Server Error');
      expect((error as ApiError).data).toBeUndefined();
    }
  });

  it('merges custom headers with defaults', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ success: true }),
    });

    await identityRequest('/custom', {
      headers: {
        'X-Custom-Header': 'value',
      },
    });

    expect(fetchMock).toHaveBeenCalledWith(
      '/custom',
      expect.objectContaining({
        headers: expect.objectContaining({
          Accept: 'application/json',
          'X-Custom-Header': 'value',
        }),
      })
    );
  });
});

describe('linkingRequest', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    global.fetch = fetchMock as unknown as typeof fetch;
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('makes request to linking service', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ connected: true }),
    });

    const result = await linkingRequest<{ connected: boolean }>('/status');

    expect(result).toEqual({ connected: true });
    expect(fetchMock).toHaveBeenCalledWith(
      '/status',
      expect.objectContaining({
        credentials: 'include',
      })
    );
  });

  it('throws ApiError on linking service error', async () => {
    fetchMock.mockResolvedValue({
      ok: false,
      status: 401,
      statusText: 'Unauthorized',
      headers: new Headers({ 'content-type': 'application/json' }),
      json: async () => ({ message: 'Invalid credentials' }),
    });

    await expect(linkingRequest('/protected')).rejects.toThrow(ApiError);
  });
});
