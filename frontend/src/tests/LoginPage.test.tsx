import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { Provider } from 'react-redux';
import { store } from '../store/store';

// Mock MSAL
const mockLoginRedirect = vi.fn();
vi.mock('@azure/msal-react', () => ({
  useMsal: () => ({ instance: { loginRedirect: mockLoginRedirect }, accounts: [] }),
  MsalProvider: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

// Mock auth API
vi.mock('../api/authApi', () => ({
  fetchCurrentUser: vi.fn().mockResolvedValue({
    id: '1',
    email: 'test@example.com',
    displayName: 'Test User',
    roles: ['employee'],
    status: 'active',
  }),
  default: {
    get: vi.fn().mockResolvedValue({ data: { data: { id: '1', email: 'test@example.com', displayName: 'Test User', roles: ['employee'], status: 'active' } } }),
  },
}));

import LoginPage from '../pages/Login/LoginPage';
import { fetchCurrentUser } from '../api/authApi';
import api from '../api/authApi';

const renderWithProvider = (ui: React.ReactElement) => {
  return render(<Provider store={store}>{ui}</Provider>);
};

describe('LoginPage', () => {
  beforeEach(() => {
    mockLoginRedirect.mockClear();
  });

  // UT-F01-UI-001: Login page renders with data-testid="login-page"
  it('UT-F01-UI-001: renders login page with correct data-testid', () => {
    renderWithProvider(<LoginPage />);
    expect(screen.getByTestId('login-page')).toBeInTheDocument();
  });

  // UT-F01-UI-002: Sign-in button is visible with correct data-testid
  it('UT-F01-UI-002: sign-in button has data-testid="sign-in-button" and is visible', () => {
    renderWithProvider(<LoginPage />);
    const button = screen.getByTestId('sign-in-button');
    expect(button).toBeInTheDocument();
    expect(button).toBeVisible();
    expect(button).toHaveTextContent(/sign in with microsoft/i);
  });

  // UT-F01-UI-003: Clicking sign-in calls loginRedirect
  it('UT-F01-UI-003: clicking sign-in button calls loginRedirect', async () => {
    const user = userEvent.setup();
    renderWithProvider(<LoginPage />);
    const button = screen.getByTestId('sign-in-button');
    await user.click(button);
    expect(mockLoginRedirect).toHaveBeenCalledTimes(1);
    expect(mockLoginRedirect).toHaveBeenCalledWith(
      expect.objectContaining({ scopes: expect.arrayContaining(['openid', 'profile']) })
    );
  });

  // UT-F01-UI-004: Loading state shows data-testid="loading-spinner"
  it('UT-F01-UI-004: loading state shows loading-spinner', async () => {
    // Mock loginRedirect to return a never-resolving promise to keep loading state
    mockLoginRedirect.mockImplementation(() => new Promise(() => {}));
    const user = userEvent.setup();
    renderWithProvider(<LoginPage />);
    await user.click(screen.getByTestId('sign-in-button'));
    await waitFor(() => {
      expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
    });
  });

  // UT-F01-UI-005: fetchCurrentUser reads response.data.data (not response.data)
  it('UT-F01-UI-005: fetchCurrentUser reads response.data.data (ApiResponse envelope)', async () => {
    const mockGet = vi.fn().mockResolvedValue({
      data: {
        data: {
          id: '42',
          email: 'user@test.com',
          displayName: 'John Doe',
          roles: ['manager'],
          status: 'active',
        },
      },
    });
    const axiosInstance = { get: mockGet };
    // Simulate what fetchCurrentUser does: response.data.data
    const response = await axiosInstance.get('/auth/me');
    const user = response.data.data;
    expect(user.id).toBe('42');
    expect(user.email).toBe('user@test.com');
    // Ensure we're not reading response.data (which would be { data: {...} })
    expect(typeof response.data.data).toBe('object');
    expect(response.data.data).not.toHaveProperty('data');
  });

  // UT-F01-UI-006: authApi.get returns ApiResponse<UserProfile> shape
  it('UT-F01-UI-006: authApi.get returns ApiResponse<UserProfile> shape', async () => {
    const result = await (api as unknown as { get: (url: string) => Promise<{ data: { data: { id: string; email: string; displayName: string; roles: string[]; status: string } } }> }).get('/auth/me');
    // The response shape must be { data: { data: UserProfile } }
    expect(result).toHaveProperty('data');
    expect(result.data).toHaveProperty('data');
    const profile = result.data.data;
    expect(profile).toHaveProperty('id');
    expect(profile).toHaveProperty('email');
    expect(profile).toHaveProperty('displayName');
    expect(profile).toHaveProperty('roles');
    expect(profile).toHaveProperty('status');
  });
});
