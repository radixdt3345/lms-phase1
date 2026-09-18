import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import {
  fetchNotifications,
  getUnreadCount,
  markAllRead,
  markRead,
  deleteNotification,
  NotificationDto,
  NotificationPageDto,
} from '../api/notificationApi';

interface NotificationState {
  notifications: NotificationDto[];
  unreadCount: number;
  totalCount: number;
  page: number;
  pageSize: number;
  loading: boolean;
  error: string | null;
}

const initialState: NotificationState = {
  notifications: [],
  unreadCount: 0,
  totalCount: 0,
  page: 1,
  pageSize: 20,
  loading: false,
  error: null,
};

export const loadNotifications = createAsyncThunk(
  'notifications/loadNotifications',
  async ({ page, pageSize }: { page: number; pageSize: number }) => {
    return fetchNotifications(page, pageSize);
  },
);

export const loadUnreadCount = createAsyncThunk(
  'notifications/loadUnreadCount',
  async () => {
    return getUnreadCount();
  },
);

export const markNotificationRead = createAsyncThunk(
  'notifications/markRead',
  async (id: string) => {
    await markRead(id);
    return id;
  },
);

export const markAllNotificationsRead = createAsyncThunk(
  'notifications/markAllRead',
  async () => {
    await markAllRead();
  },
);

export const removeNotification = createAsyncThunk(
  'notifications/delete',
  async (id: string) => {
    await deleteNotification(id);
    return id;
  },
);

const notificationSlice = createSlice({
  name: 'notifications',
  initialState,
  reducers: {
    setPage(state, action: PayloadAction<number>) {
      state.page = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      // loadNotifications
      .addCase(loadNotifications.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(loadNotifications.fulfilled, (state, action) => {
        state.loading = false;
        const page = action.payload as NotificationPageDto;
        state.notifications = page.items;
        state.totalCount = page.totalCount;
        state.page = page.pageNumber;
        state.pageSize = page.pageSize;
      })
      .addCase(loadNotifications.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message ?? 'Failed to load notifications';
      })
      // loadUnreadCount
      .addCase(loadUnreadCount.fulfilled, (state, action) => {
        state.unreadCount = action.payload;
      })
      // markNotificationRead
      .addCase(markNotificationRead.fulfilled, (state, action) => {
        const n = state.notifications.find((x) => x.id === action.payload);
        if (n && !n.isRead) {
          n.isRead = true;
          state.unreadCount = Math.max(0, state.unreadCount - 1);
        }
      })
      // markAllNotificationsRead
      .addCase(markAllNotificationsRead.fulfilled, (state) => {
        state.notifications.forEach((n) => { n.isRead = true; });
        state.unreadCount = 0;
      })
      // removeNotification
      .addCase(removeNotification.fulfilled, (state, action) => {
        const idx = state.notifications.findIndex((x) => x.id === action.payload);
        if (idx !== -1) {
          const wasUnread = !state.notifications[idx].isRead;
          state.notifications.splice(idx, 1);
          state.totalCount = Math.max(0, state.totalCount - 1);
          if (wasUnread) state.unreadCount = Math.max(0, state.unreadCount - 1);
        }
      });
  },
});

export const { setPage } = notificationSlice.actions;
export default notificationSlice.reducer;
