import React, { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from '../pages/Login/LoginPage';
import ProtectedRoute from '../components/ProtectedRoute';
import { CircularProgress, Box } from '@mui/material';

const DepartmentListPage = lazy(() => import('../pages/Departments/DepartmentListPage'));
const LeaveTypeListPage = lazy(() => import('../pages/LeavePolicy/LeaveTypeListPage'));
const AuditTrailPage = lazy(() => import('../pages/AuditTrail/AuditTrailPage'));
const EmployeeListPage = lazy(() => import('../pages/Employees/EmployeeListPage'));
const PublicHolidayListPage = lazy(() => import('../pages/PublicHolidays/PublicHolidayListPage'));
const LeaveRequestListPage = lazy(() => import('../pages/LeaveRequests/LeaveRequestListPage'));
const CompOffListPage = lazy(() => import('../pages/CompOff/CompOffListPage'));
const NotificationsPage = lazy(() => import('../pages/Notifications/NotificationsPage'));
const JobAdminPage = lazy(() => import('../pages/Jobs/JobAdminPage'));

const DashboardPlaceholder: React.FC = () => (
  <div>Dashboard - Coming Soon</div>
);

const AppRouter: React.FC = () => {
  return (
    <BrowserRouter>
      <Suspense fallback={<Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}><CircularProgress /></Box>}>
        <Routes>
          {/* Public route */}
          <Route path="/login" element={<LoginPage />} />

          {/* Protected routes — require Azure AD authentication */}
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <DashboardPlaceholder />
              </ProtectedRoute>
            }
          />
          <Route
            path="/departments"
            element={
              <ProtectedRoute>
                <DepartmentListPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/leave-types"
            element={
              <ProtectedRoute>
                <LeaveTypeListPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/audit-trail"
            element={
              <ProtectedRoute>
                <AuditTrailPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/employees"
            element={
              <ProtectedRoute>
                <EmployeeListPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/public-holidays"
            element={
              <ProtectedRoute>
                <PublicHolidayListPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/leave-requests"
            element={
              <ProtectedRoute>
                <LeaveRequestListPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/comp-off"
            element={
              <ProtectedRoute>
                <CompOffListPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/notifications"
            element={
              <ProtectedRoute>
                <NotificationsPage />
              </ProtectedRoute>
            }
          />
          {/* Admin-only routes — HRAdmin / SuperAdmin */}
          <Route
            path="/admin/jobs"
            element={
              <ProtectedRoute>
                <JobAdminPage />
              </ProtectedRoute>
            }
          />

          {/* Catch-all — redirect to login */}
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  );
};

export default AppRouter;
