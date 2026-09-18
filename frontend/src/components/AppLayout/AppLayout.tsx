import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  Box,
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  AppBar,
  Typography,
  IconButton,
  Divider,
  Tooltip,
} from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
import DashboardIcon from '@mui/icons-material/Dashboard';
import PeopleIcon from '@mui/icons-material/People';
import BusinessIcon from '@mui/icons-material/Business';
import EventBusyIcon from '@mui/icons-material/EventBusy';
import AssignmentIcon from '@mui/icons-material/Assignment';
import BalanceIcon from '@mui/icons-material/AccountBalance';
import BeachAccessIcon from '@mui/icons-material/BeachAccess';
import PublicIcon from '@mui/icons-material/Public';
import NotificationsIcon from '@mui/icons-material/Notifications';
import HistoryIcon from '@mui/icons-material/History';
import WorkIcon from '@mui/icons-material/Work';
import ApprovalIcon from '@mui/icons-material/HowToReg';

const DRAWER_WIDTH = 240;

interface NavItem {
  label: string;
  path: string;
  icon: React.ReactNode;
  roles?: string[];
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', path: '/dashboard', icon: <DashboardIcon /> },
  { label: 'Leave Requests', path: '/leave-requests', icon: <AssignmentIcon /> },
  { label: 'Leave Balances', path: '/leave-balances', icon: <BalanceIcon /> },
  { label: 'Comp Off', path: '/comp-off', icon: <BeachAccessIcon /> },
  { label: 'Notifications', path: '/notifications', icon: <NotificationsIcon /> },
];

const APPROVER_NAV_ITEMS: NavItem[] = [
  { label: 'Approvals', path: '/approvals', icon: <ApprovalIcon />, roles: ['Manager', 'HRAdmin', 'SuperAdmin'] },
];

const ADMIN_NAV_ITEMS: NavItem[] = [
  { label: 'Employees', path: '/employees', icon: <PeopleIcon />, roles: ['HRAdmin', 'SuperAdmin'] },
  { label: 'Departments', path: '/departments', icon: <BusinessIcon />, roles: ['HRAdmin', 'SuperAdmin'] },
  { label: 'Leave Types', path: '/leave-types', icon: <EventBusyIcon />, roles: ['HRAdmin', 'SuperAdmin'] },
  { label: 'Public Holidays', path: '/public-holidays', icon: <PublicIcon />, roles: ['HRAdmin', 'SuperAdmin'] },
  { label: 'Audit Trail', path: '/audit-trail', icon: <HistoryIcon />, roles: ['HRAdmin', 'SuperAdmin'] },
  { label: 'Job Admin', path: '/admin/jobs', icon: <WorkIcon />, roles: ['HRAdmin', 'SuperAdmin'] },
];

interface AppLayoutProps {
  children: React.ReactNode;
  userRoles?: string[];
}

const AppLayout: React.FC<AppLayoutProps> = ({ children, userRoles = [] }) => {
  const navigate = useNavigate();
  const location = useLocation();
  const [mobileOpen, setMobileOpen] = useState(false);

  const isApprover = userRoles.some((r) => ['Manager', 'HRAdmin', 'SuperAdmin'].includes(r));
  const isHRAdmin = userRoles.some((r) => ['HRAdmin', 'SuperAdmin'].includes(r));

  const drawerContent = (
    <Box data-testid="sidebar-nav">
      <Toolbar>
        <Typography variant="h6" noWrap component="div" fontWeight={700} color="primary">
          LMS
        </Typography>
      </Toolbar>
      <Divider />
      <List>
        {NAV_ITEMS.map((item) => (
          <Tooltip key={item.path} title={item.label} placement="right" disableHoverListener>
            <ListItemButton
              data-testid={`nav-${item.label.toLowerCase().replace(/\s+/g, '-')}`}
              selected={location.pathname === item.path}
              onClick={() => {
                navigate(item.path);
                setMobileOpen(false);
              }}
              sx={{ borderRadius: 1, mx: 0.5, mb: 0.25 }}
            >
              <ListItemIcon sx={{ minWidth: 36 }}>{item.icon}</ListItemIcon>
              <ListItemText primary={item.label} primaryTypographyProps={{ fontSize: 14 }} />
            </ListItemButton>
          </Tooltip>
        ))}
      </List>
      {isApprover && (
        <>
          <Divider sx={{ my: 1 }} />
          <Typography
            variant="caption"
            sx={{ px: 2, color: 'text.secondary', textTransform: 'uppercase', letterSpacing: 0.5 }}
          >
            Approvals
          </Typography>
          <List>
            {APPROVER_NAV_ITEMS.map((item) => (
              <Tooltip key={item.path} title={item.label} placement="right" disableHoverListener>
                <ListItemButton
                  data-testid={`nav-${item.label.toLowerCase().replace(/\s+/g, '-')}`}
                  selected={location.pathname === item.path}
                  onClick={() => {
                    navigate(item.path);
                    setMobileOpen(false);
                  }}
                  sx={{ borderRadius: 1, mx: 0.5, mb: 0.25 }}
                >
                  <ListItemIcon sx={{ minWidth: 36 }}>{item.icon}</ListItemIcon>
                  <ListItemText primary={item.label} primaryTypographyProps={{ fontSize: 14 }} />
                </ListItemButton>
              </Tooltip>
            ))}
          </List>
        </>
      )}
      {isHRAdmin && (
        <>
          <Divider sx={{ my: 1 }} />
          <Typography
            variant="caption"
            sx={{ px: 2, color: 'text.secondary', textTransform: 'uppercase', letterSpacing: 0.5 }}
          >
            Admin
          </Typography>
          <List>
            {ADMIN_NAV_ITEMS.map((item) => (
              <Tooltip key={item.path} title={item.label} placement="right" disableHoverListener>
                <ListItemButton
                  data-testid={`nav-admin-${item.label.toLowerCase().replace(/\s+/g, '-')}`}
                  selected={location.pathname === item.path}
                  onClick={() => {
                    navigate(item.path);
                    setMobileOpen(false);
                  }}
                  sx={{ borderRadius: 1, mx: 0.5, mb: 0.25 }}
                >
                  <ListItemIcon sx={{ minWidth: 36 }}>{item.icon}</ListItemIcon>
                  <ListItemText primary={item.label} primaryTypographyProps={{ fontSize: 14 }} />
                </ListItemButton>
              </Tooltip>
            ))}
          </List>
        </>
      )}
    </Box>
  );

  return (
    <Box sx={{ display: 'flex' }} data-testid="app-layout">
      {/* Top AppBar (mobile) */}
      <AppBar
        position="fixed"
        sx={{ display: { sm: 'none' }, zIndex: (theme) => theme.zIndex.drawer + 1 }}
      >
        <Toolbar>
          <IconButton
            color="inherit"
            aria-label="open drawer"
            edge="start"
            onClick={() => setMobileOpen(true)}
            data-testid="sidebar-toggle"
          >
            <MenuIcon />
          </IconButton>
          <Typography variant="h6" noWrap component="div">
            Leave Management System
          </Typography>
        </Toolbar>
      </AppBar>

      {/* Mobile Drawer */}
      <Drawer
        variant="temporary"
        open={mobileOpen}
        onClose={() => setMobileOpen(false)}
        ModalProps={{ keepMounted: true }}
        sx={{
          display: { xs: 'block', sm: 'none' },
          '& .MuiDrawer-paper': { boxSizing: 'border-box', width: DRAWER_WIDTH },
        }}
      >
        {drawerContent}
      </Drawer>

      {/* Permanent Sidebar (desktop) */}
      <Drawer
        variant="permanent"
        sx={{
          display: { xs: 'none', sm: 'block' },
          width: DRAWER_WIDTH,
          flexShrink: 0,
          '& .MuiDrawer-paper': { boxSizing: 'border-box', width: DRAWER_WIDTH },
        }}
        open
      >
        {drawerContent}
      </Drawer>

      {/* Main content */}
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 0,
          width: { sm: `calc(100% - ${DRAWER_WIDTH}px)` },
          minHeight: '100vh',
        }}
      >
        {/* Spacer for mobile AppBar */}
        <Box sx={{ display: { sm: 'none' }, ...{ minHeight: 56 } }} />
        {children}
      </Box>
    </Box>
  );
};

export default AppLayout;
