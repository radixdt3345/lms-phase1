import React, { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from '../pages/Login/LoginPage';
import ProtectedRoute from '../components/ProtectedRoute';
import AppLayout from '../components/AppLayout/AppLayout';
import { CircularProgress, Box } from '@mui/material';
import { useSelector } from 'react-redux';
import type { RootState } from '../store/store';

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
const ApprovalDashboardPage = lazy(() => import('../pages/Approvals/ApprovalDashboardPage'));
const DashboardPage = lazy(() => import('../pages/Dashboard/DashboardPage'));
const ReportsPage = lazy(() => import('../pages/Reports/ReportsPage'));

const LoadingFallback = (
  <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
    <CircularProgress />
  </Box>
);

/** Wraps a page with auth guard + sidebar layout. */
const LayoutRoute: React.FC<{ children: React.ReactNode; requiredRoles?: string[] }> = ({
  children,
  requiredRoles,
}) => {
  const userRoles: string[] = useSelector(
    (state: RootState) => (state.auth as any)?.user?.roles ?? []
  );
  return (
    <ProtectedRoute requiredRoles={requiredRoles}>
      <AppLayout userRoles={userRoles}>{children}</AppLayout>
    </ProtectedRoute>
  );
};

const AppRouter: React.FC = () => {
  return (
    <BrowserRouter>
      <Suspense fallback={LoadingFallback}>
        <Routes>
          {/* Public route */}
          <Route path="/login" element={<LoginPage />} />

          {/* Protected routes — require Azure AD authentication */}
          <Route
            path="/dashboard"
            element={
              <LayoutRoute requiredRoles={['Manager', 'HRAdmin', 'SuperAdmin']}>
                <DashboardPage />
              </LayoutRoute>
            }
          />
          <Route
            path="/departments"
            element={
              <LayoutRoute>
                <DepartmentListPage />
              </LayoutRoute>
            }
          />
          <Route
            path="/leave-types"
            element={
              <LayoutRoute>
                <LeaveTypeListPage />
              </LayoutRoute>
            }
          />
          <Route
            path="/audit-trail"
            element={
              <LayoutRoute>
                <AuditTrailPage />
              </LayoutRoute>
            }
          />
          <Route
            path="/employees"
            element={
              <LayoutRoute>
                <EmployeeListPage />
              </LayoutRoute>
            }
          />
          <Route
            path="/public-holidays"
            element={
              <LayoutRoute>
                <PublicHolidayListPage />
              </LayoutRoute>
            }
          />
          <Route
            path="/leave-requests"
            element={
              <LayoutRoute>
                <LeaveRequestListPage />
              </LayoutRoute>
            }
          />
          <Route
            path="/comp-off"
            element={
              <LayoutRoute>
                <CompOffListPage />
              </LayoutRoute>
            }
          />
          <Route
            path="/notifications"
            element={
              <LayoutRoute>
                <NotificationsPage />
              </LayoutRoute>
            }
          />

          {/* Admin-only routes — HRAdmin / SuperAdmin */}
          <Route
            path="/admin/jobs"
            element={
              <LayoutRoute requiredRoles={['HRAdmin', 'SuperAdmin']}>
                <JobAdminPage />
              </LayoutRoute>
            }
          />
          {/* /jobs alias — canonical path used by sidebar navigation */}
          <Route
            path="/jobs"
            element={
              <LayoutRoute requiredRoles={['HRAdmin', 'SuperAdmin']}>
                <JobAdminPage />
              </LayoutRoute>
            }
          />

          {/* Manager/Admin approval workflow route — F-08 */}
          <Route
            path="/approvals"
            element={
              <LayoutRoute requiredRoles={['Manager', 'HRAdmin', 'SuperAdmin']}>
                <ApprovalDashboardPage />
              </LayoutRoute>
            }
          />

          {/* F-05: Leave Balance — canonical /leave-balances route (INT layer) */}
          <Route
            path="/leave-balances"
            element={
              <LayoutRoute>
                <LeaveBalancePage />
              </LayoutRoute>
            }
          />
          {/* Legacy /leave-balance → redirect to canonical /leave-balances */}
          <Route
            path="/leave-balance"
            element={<Navigate to="/leave-balances" replace />}
          />

          {/* F-11: Dashboard — Manager, HRAdmin, SuperAdmin */}
          <Route
            path="/dashboard"
            element={
              <LayoutRoute requiredRoles={['Manager', 'HRAdmin', 'SuperAdmin']}>
                <DashboardPage />
              </LayoutRoute>
            }
          />

          {/* F-12: Reports & CSV Export — HRAdmin, SuperAdmin */}
          <Route
            path="/reports"
            element={
              <LayoutRoute requiredRoles={['HRAdmin', 'SuperAdmin']}>
                <ReportsPage />
              </LayoutRoute>
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
