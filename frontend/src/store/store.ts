import { configureStore } from '@reduxjs/toolkit';
import authReducer from './authSlice';
import departmentReducer from './departmentSlice';
import leavePolicyReducer from './leavePolicySlice';
import auditLogReducer from './auditLogSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    departments: departmentReducer,
    leavePolicy: leavePolicyReducer,
    auditLog: auditLogReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
