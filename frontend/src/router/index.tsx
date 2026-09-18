import React, { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from '../pages/Login/LoginPage';
import ProtectedRoute from '../components/ProtectedRoute';
import MainLayout from '../components/MainLayout/MainLayout';
import { CircularProgress, Box } from '@mui/material';

const DepartmentListPage = lazy(() => import('../pages/Departments/DepartmentListPage'));
const LeaveTypeListPage = lazy(() => import('../pages/LeavePolicy/LeaveTypeListPage'));
const AuditTrailPage = lazy(() => import('../pages/AuditTrail/AuditTrailPage'));
const EmployeeListPage = lazy(() => import('../pages/Employees/EmployeeListPage'));
const PublicHolidayListPage = lazy(() => import('../pages/PublicHolidays/PublicHolidayListPage'));
const LeaveRequestListPage = lazy(() => import('../pages/LeaveRequests/LeaveRequestListPage'));
const CompOffListPage = lazy(() => import('../pages/CompOff/CompOffListPage'));
const LeaveBalancePage = lazy(() => import('../pages/LeaveBalance/LeaveBalancePage'));
const NotificationsPage = lazy(() => import('../pages/Notifications/NotificationsPage'));
const JobAdminPage = lazy(() => import('../pages/Jobs/JobAdminPage'));

const DashboardPlaceholder: React.FC = () => (
  <div style={{ padding: 24 }}>Dashboard - Coming Soon</div>
);

const Fallback: React.FC = () => (
  <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
    <CircularProgress />
  </Box>
);

const AppRouter: React.FC = () => {
  return (
    <BrowserRouter>
      <Suspense fallback={<Fallback />}>
        <Routes>
          {/* Public route */}
          <Route path="/login" element={<LoginPage />} />

          {/* Protected routes wrapped in MainLayout (sidebar + AppBar) */}
          <Route
            element={
              <ProtectedRoute>
                <MainLayout />
              </ProtectedRoute>
            }
          >
            <Route path="/dashboard" element={<DashboardPlaceholder />} />
            <Route path="/departments" element={<DepartmentListPage />} />
            <Route path="/leave-types" element={<LeaveTypeListPage />} />
            <Route path="/audit-trail" element={<AuditTrailPage />} />
            <Route path="/employees" element={<EmployeeListPage />} />
            <Route path="/public-holidays" element={<PublicHolidayListPage />} />
            <Route path="/leave-requests" element={<LeaveRequestListPage />} />
            {/* F-07 Comp-Off Management — INT wired here */}
            <Route path="/comp-off" element={<CompOffListPage />} />
            {/* F-09 Notifications — INT wired here */}
            <Route path="/notifications" element={<NotificationsPage />} />
            <Route path="/leave-balance" element={<LeaveBalancePage />} />
            {/* Admin-only routes — HRAdmin / SuperAdmin */}
            <Route
              path="/admin/jobs"
              element={
                <ProtectedRoute requiredRoles={['HRAdmin', 'SuperAdmin']}>
                  <JobAdminPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/jobs"
              element={
                <ProtectedRoute requiredRoles={['HRAdmin', 'SuperAdmin']}>
                  <JobAdminPage />
                </ProtectedRoute>
              }
            />
          </Route>

          {/* Catch-all — redirect to login */}
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  );
};

export default AppRouter;
