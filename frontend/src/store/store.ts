import { configureStore } from '@reduxjs/toolkit';
import authReducer from './authSlice';
import departmentReducer from './departmentSlice';
import leavePolicyReducer from './leavePolicySlice';
import auditLogReducer from './auditLogSlice';
import employeeReducer from './employeeSlice';
import publicHolidayReducer from './publicHolidaySlice';
import leaveRequestReducer from './leaveRequestSlice';
import compOffReducer from './compOffSlice';
import leaveBalanceReducer from './leaveBalanceSlice';
import notificationReducer from './notificationSlice';
import jobReducer from './jobSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    departments: departmentReducer,
    leavePolicy: leavePolicyReducer,
    auditLog: auditLogReducer,
    employees: employeeReducer,
    publicHolidays: publicHolidayReducer,
    leaveRequests: leaveRequestReducer,
    compOff: compOffReducer,
    leaveBalance: leaveBalanceReducer,
    notifications: notificationReducer,
    jobs: jobReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
