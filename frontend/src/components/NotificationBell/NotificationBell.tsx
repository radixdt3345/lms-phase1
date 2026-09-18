import React, { useEffect, useState } from 'react';
import { Badge, IconButton } from '@mui/material';
import NotificationsIcon from '@mui/icons-material/Notifications';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState, AppDispatch } from '../../store/store';
import { loadUnreadCount } from '../../store/notificationSlice';
import NotificationDropdown from './NotificationDropdown';

const POLL_INTERVAL_MS = 60_000;

const NotificationBell: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const unreadCount = useSelector((s: RootState) => s.notifications.unreadCount);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);

  useEffect(() => {
    dispatch(loadUnreadCount());
    const timer = setInterval(() => {
      dispatch(loadUnreadCount());
    }, POLL_INTERVAL_MS);
    return () => clearInterval(timer);
  }, [dispatch]);

  const handleOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  return (
    <>
      <IconButton
        data-testid="notification-bell"
        color="inherit"
        onClick={handleOpen}
        aria-label="notifications"
      >
        <Badge
          badgeContent={unreadCount > 0 ? unreadCount : undefined}
          color="error"
          data-testid="unread-badge"
        >
          <NotificationsIcon />
        </Badge>
      </IconButton>
      <NotificationDropdown anchorEl={anchorEl} onClose={handleClose} />
    </>
  );
};

export default NotificationBell;
