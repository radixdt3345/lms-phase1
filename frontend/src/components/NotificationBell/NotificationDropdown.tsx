import React, { useEffect } from 'react';
import {
  Popover,
  Box,
  Typography,
  List,
  ListItem,
  ListItemText,
  Button,
  Divider,
  CircularProgress,
} from '@mui/material';
import { useDispatch, useSelector } from 'react-redux';
import { useNavigate } from 'react-router-dom';
import type { RootState, AppDispatch } from '../../store/store';
import {
  loadNotifications,
  markAllNotificationsRead,
  loadUnreadCount,
} from '../../store/notificationSlice';

interface Props {
  anchorEl: HTMLElement | null;
  onClose: () => void;
}

const NotificationDropdown: React.FC<Props> = ({ anchorEl, onClose }) => {
  const dispatch = useDispatch<AppDispatch>();
  const navigate = useNavigate();
  const { notifications, loading } = useSelector((s: RootState) => s.notifications);
  const open = Boolean(anchorEl);

  useEffect(() => {
    if (open) {
      dispatch(loadNotifications({ page: 1, pageSize: 10 }));
    }
  }, [open, dispatch]);

  const handleMarkAllRead = async () => {
    await dispatch(markAllNotificationsRead());
    dispatch(loadUnreadCount());
  };

  const handleSeeAll = () => {
    onClose();
    navigate('/notifications');
  };

  return (
    <Popover
      open={open}
      anchorEl={anchorEl}
      onClose={onClose}
      anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      transformOrigin={{ vertical: 'top', horizontal: 'right' }}
      data-testid="notification-dropdown"
    >
      <Box sx={{ width: 360, maxHeight: 480 }}>
        {/* Header */}
        <Box
          sx={{
            px: 2,
            py: 1.5,
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <Typography variant="h6" fontWeight="bold">
            Notifications
          </Typography>
          <Button
            size="small"
            onClick={handleMarkAllRead}
            data-testid="mark-all-read-btn"
          >
            Mark all read
          </Button>
        </Box>

        <Divider />

        {/* Body */}
        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
            <CircularProgress size={24} />
          </Box>
        ) : notifications.length === 0 ? (
          <Typography sx={{ p: 3, textAlign: 'center', color: 'text.secondary' }}>
            No notifications
          </Typography>
        ) : (
          <List disablePadding sx={{ overflowY: 'auto', maxHeight: 360 }}>
            {notifications.slice(0, 10).map((n) => (
              <React.Fragment key={n.id}>
                <ListItem
                  data-testid={`notification-item-${n.id}`}
                  sx={{
                    bgcolor: n.isRead ? 'transparent' : 'action.hover',
                    alignItems: 'flex-start',
                  }}
                >
                  <ListItemText
                    primary={
                      <Typography
                        variant="body2"
                        fontWeight={n.isRead ? 'normal' : 'bold'}
                      >
                        {n.title}
                      </Typography>
                    }
                    secondary={
                      <>
                        <Typography variant="caption" display="block" color="text.secondary">
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

        {/* Footer */}
        <Box sx={{ p: 1, textAlign: 'center' }}>
          <Button size="small" onClick={handleSeeAll}>
            See all notifications
          </Button>
        </Box>
      </Box>
    </Popover>
  );
};

export default NotificationDropdown;
