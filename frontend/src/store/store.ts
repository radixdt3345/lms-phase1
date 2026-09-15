import { configureStore } from '@reduxjs/toolkit';
import authReducer from './authSlice';
import departmentReducer from './departmentSlice';
import leavePolicyReducer from './leavePolicySlice';
import publicHolidayReducer from './publicHolidaySlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    departments: departmentReducer,
    leavePolicy: leavePolicyReducer,
    publicHolidays: publicHolidayReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
