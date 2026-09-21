import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { MemoryRouter } from 'react-router-dom';
import NotificationBell from '../components/NotificationBell/NotificationBell';
import notificationReducer from '../store/notificationSlice';

// Mock the API module
vi.mock('../api/notificationApi', () => ({
  fetchNotifications: vi.fn().mockResolvedValue({
    items: [
      {
        id: 'n1',
        userId: 'u1',
        title: 'Leave approved',
        body: 'Your leave request has been approved.',
        type: 'LeaveApproval',
        isRead: false,
        createdAt: '2024-01-15T10:00:00Z',
      },
      {
        id: 'n2',
        userId: 'u1',
        title: 'Policy updated',
        body: 'Leave policy has been updated.',
        type: 'PolicyUpdate',
        isRead: true,
        createdAt: '2024-01-14T09:00:00Z',
      },
    ],
    totalCount: 2,
    pageNumber: 1,
    pageSize: 10,
    totalPages: 1,
  }),
  getUnreadCount: vi.fn().mockResolvedValue(3),
  markRead: vi.fn().mockResolvedValue(true),
  markAllRead: vi.fn().mockResolvedValue(true),
  deleteNotification: vi.fn().mockResolvedValue(true),
}));

function makeStore(preloaded?: Partial<ReturnType<typeof notificationReducer>>) {
  return configureStore({
    reducer: { notifications: notificationReducer },
    preloadedState: preloaded ? { notifications: preloaded as ReturnType<typeof notificationReducer> } : undefined,
  });
}

function renderBell(store = makeStore()) {
  return render(
    <Provider store={store}>
      <MemoryRouter>
        <NotificationBell />
      </MemoryRouter>
    </Provider>,
  );
}

// UT-FE-N-01: Bell icon renders
test('UT-FE-N-01: renders notification bell icon button', () => {
  renderBell();
  expect(screen.getByTestId('notification-bell')).toBeInTheDocument();
});

// UT-FE-N-02: Unread badge shows correct count from store
test('UT-FE-N-02: displays unread count badge when unread > 0', () => {
  const store = makeStore({
    notifications: [],
    unreadCount: 5,
    totalCount: 5,
    page: 1,
    pageSize: 20,
    loading: false,
    error: null,
  });
  renderBell(store);
  expect(screen.getByTestId('unread-badge')).toBeInTheDocument();
  // MUI Badge renders the count inside an aria-label or visually
  expect(screen.getByText('5')).toBeInTheDocument();
});

// UT-FE-N-03: No badge when unreadCount is 0
test('UT-FE-N-03: does not show badge content when unreadCount is 0', () => {
  const store = makeStore({
    notifications: [],
    unreadCount: 0,
    totalCount: 0,
    page: 1,
    pageSize: 20,
    loading: false,
    error: null,
  });
  renderBell(store);
  expect(screen.queryByText('0')).not.toBeInTheDocument();
});

// UT-FE-N-04: Clicking bell opens the dropdown
test('UT-FE-N-04: clicking bell opens notification dropdown', async () => {
  renderBell();
  fireEvent.click(screen.getByTestId('notification-bell'));
  await waitFor(() => {
    expect(screen.getByTestId('notification-dropdown')).toBeInTheDocument();
  });
});

// UT-FE-N-05: Mark all read button appears in dropdown
test('UT-FE-N-05: mark-all-read button is visible in dropdown', async () => {
  renderBell();
  fireEvent.click(screen.getByTestId('notification-bell'));
  await waitFor(() => {
    expect(screen.getByTestId('mark-all-read-btn')).toBeInTheDocument();
  });
});

// UT-FE-N-06: Clicking "Mark all read" calls markAllRead API
test('UT-FE-N-06: clicking mark-all-read dispatches markAllNotificationsRead', async () => {
  const notifApi = await import('../api/notificationApi');
  const { markAllRead } = notifApi;
  renderBell();
  fireEvent.click(screen.getByTestId('notification-bell'));
  await waitFor(() => {
    expect(screen.getByTestId('mark-all-read-btn')).toBeInTheDocument();
  });
  fireEvent.click(screen.getByTestId('mark-all-read-btn'));
  await waitFor(() => {
    expect(markAllRead).toHaveBeenCalled();
  });
});

// UT-FE-N-07: Notification items render with correct testids
test('UT-FE-N-07: notification items render with data-testid after opening dropdown', async () => {
  renderBell();
  fireEvent.click(screen.getByTestId('notification-bell'));
  await waitFor(() => {
    expect(screen.getByTestId('notification-item-n1')).toBeInTheDocument();
    expect(screen.getByTestId('notification-item-n2')).toBeInTheDocument();
  });
});

// UT-FE-N-08: Error state — unread count falls back gracefully
test('UT-FE-N-08: renders without crashing when notifications list is empty', () => {
  const store = makeStore({
    notifications: [],
    unreadCount: 0,
    totalCount: 0,
    page: 1,
    pageSize: 20,
    loading: false,
    error: 'Network error',
  });
  renderBell(store);
  expect(screen.getByTestId('notification-bell')).toBeInTheDocument();
});
