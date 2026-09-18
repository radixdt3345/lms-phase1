import React, { useEffect, useState } from 'react';
import {
  Box,
  Typography,
  List,
  ListItem,
  ListItemText,
  Divider,
  ToggleButton,
  ToggleButtonGroup,
  CircularProgress,
  Pagination,
  Alert,
  IconButton,
  Tooltip,
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import CheckIcon from '@mui/icons-material/Check';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState, AppDispatch } from '../../store/store';
import {
  loadNotifications,
  markNotificationRead,
  removeNotification,
  markAllNotificationsRead,
  loadUnreadCount,
} from '../../store/notificationSlice';

type FilterValue = 'all' | 'unread' | 'read';

const PAGE_SIZE = 20;

const NotificationsPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { notifications, loading, error, totalCount, page } = useSelector(
    (s: RootState) => s.notifications,
  );
  const [filter, setFilter] = useState<FilterValue>('all');

  useEffect(() => {
    dispatch(loadNotifications({ page: 1, pageSize: PAGE_SIZE }));
  }, [dispatch]);

  const handlePageChange = (_: React.ChangeEvent<unknown>, value: number) => {
    dispatch(loadNotifications({ page: value, pageSize: PAGE_SIZE }));
  };

  const handleMarkRead = (id: string) => {
    dispatch(markNotificationRead(id)).then(() => dispatch(loadUnreadCount()));
  };

  const handleDelete = (id: string) => {
    dispatch(removeNotification(id));
  };

  const handleMarkAll = () => {
    dispatch(markAllNotificationsRead()).then(() => dispatch(loadUnreadCount()));
  };

  const displayed = notifications.filter((n) => {
    if (filter === 'unread') return !n.isRead;
    if (filter === 'read') return n.isRead;
    return true;
  });

  const totalPages = Math.ceil(totalCount / PAGE_SIZE);

  return (
    <Box sx={{ maxWidth: 800, mx: 'auto', p: 3 }} data-testid="notification-list-page">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h4">Notifications</Typography>
        <Tooltip title="Mark all as read">
          <IconButton onClick={handleMarkAll} data-testid="mark-all-read-btn" color="primary">
            <CheckIcon />
          </IconButton>
        </Tooltip>
      </Box>

      <ToggleButtonGroup
        value={filter}
        exclusive
        onChange={(_, v) => v && setFilter(v)}
        size="small"
        sx={{ mb: 2 }}
        data-testid="notification-filter"
      >
        <ToggleButton value="all">All</ToggleButton>
        <ToggleButton value="unread">Unread</ToggleButton>
        <ToggleButton value="read">Read</ToggleButton>
      </ToggleButtonGroup>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
          <CircularProgress />
        </Box>
      ) : displayed.length === 0 ? (
        <Typography color="text.secondary" sx={{ textAlign: 'center', mt: 4 }}>
          No notifications to display.
        </Typography>
      ) : (
        <List disablePadding>
          {displayed.map((n) => (
            <React.Fragment key={n.id}>
              <ListItem
                data-testid={`notification-item-${n.id}`}
                sx={{ bgcolor: n.isRead ? 'transparent' : 'action.hover' }}
                secondaryAction={
                  <Box>
                    {!n.isRead && (
                      <Tooltip title="Mark as read">
                        <IconButton
                          size="small"
                          onClick={() => handleMarkRead(n.id)}
                          aria-label="mark as read"
                        >
                          <CheckIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                    <Tooltip title="Delete">
                      <IconButton
                        size="small"
                        onClick={() => handleDelete(n.id)}
                        aria-label="delete notification"
                      >
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </Box>
                }
              >
                <ListItemText
                  primary={
                    <Typography fontWeight={n.isRead ? 'normal' : 'bold'}>
                      {n.title}
                    </Typography>
                  }
                  secondary={
                    <>
                      <Typography variant="body2" color="text.secondary">
                        {n.body}
                      </Typography>
                      <Typography variant="caption" color="text.disabled">
                        {new Date(n.createdAt).toLocaleString()}
                      </Typography>
                    </>
                  }
                />
              </ListItem>
              <Divider component="li" />
            </React.Fragment>
          ))}
        </List>
      )}

      {!loading && totalPages > 1 && (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
          <Pagination
            count={totalPages}
            page={page}
            onChange={handlePageChange}
            color="primary"
          />
        </Box>
      )}
    </Box>
  );
};

export default NotificationsPage;
