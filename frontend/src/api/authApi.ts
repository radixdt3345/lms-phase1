import axios from 'axios';
import { PublicClientApplication, InteractionRequiredAuthError } from '@azure/msal-browser';
import { msalConfig, loginRequest } from '../auth/msalConfig';
import type { ApiResponse, UserProfile } from '../types';

const api = axios.create({ baseURL: import.meta.env.VITE_API_BASE_URL || '/api' });

// Lazily share the same MSAL instance used by App.tsx via a module-level singleton.
// We create a second instance here only to call acquireTokenSilent; the cache
// (sessionStorage) is shared, so both instances see the same tokens.
let _msalInstance: PublicClientApplication | null = null;

function getMsalInstance(): PublicClientApplication {
  if (!_msalInstance) {
    _msalInstance = new PublicClientApplication(msalConfig);
  }
  return _msalInstance;
}

/**
 * Axios request interceptor — attaches the MSAL Bearer token to every request.
 * Attempts a silent token acquisition first; falls back to an interaction redirect
 * when silent acquisition is not possible (e.g. no cached token).
 */
api.interceptors.request.use(async (config) => {
  try {
    const msalInstance = getMsalInstance();
    await msalInstance.initialize();

    const accounts = msalInstance.getAllAccounts();
    if (accounts.length === 0) {
      // No signed-in account — let the request go unauthenticated so
      // ProtectedRoute can redirect to /login first.
      return config;
    }

    const tokenResponse = await msalInstance.acquireTokenSilent({
      ...loginRequest,
      account: accounts[0],
    });

    config.headers = config.headers ?? {};
    config.headers['Authorization'] = `Bearer ${tokenResponse.accessToken}`;
  } catch (err) {
    if (err instanceof InteractionRequiredAuthError) {
      // Token expired and cannot be silently refreshed — redirect to login.
      const msalInstance = getMsalInstance();
      await msalInstance.loginRedirect(loginRequest);
    }
    // For other errors, let the request proceed without a token;
    // the server will respond with 401 which the UI handles.
    console.warn('[authApi] Could not acquire token silently:', err);
  }

  return config;
});

// Reads response.data.data (ApiResponse<T> envelope)
export const fetchCurrentUser = async (): Promise<UserProfile> => {
  const response = await api.get<ApiResponse<UserProfile>>('/auth/me');
  return response.data.data;
};

export const fetchAllUsers = async (): Promise<UserProfile[]> => {
  const response = await api.get<ApiResponse<UserProfile[]>>('/auth/users');
  return response.data.data;
};

export default api;
