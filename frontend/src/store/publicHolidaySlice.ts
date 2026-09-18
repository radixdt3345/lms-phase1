import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import * as publicHolidayApi from '../api/publicHolidayApi';
import type { PublicHolidayDto, CreatePublicHolidayDto, UpdatePublicHolidayDto } from '../api/publicHolidayApi';

interface PublicHolidayState {
  holidays: PublicHolidayDto[];
  loading: boolean;
  error: string | null;
  selectedYear: number;
}

const currentYear = new Date().getFullYear();
const initialState: PublicHolidayState = {
  holidays: [],
  loading: false,
  error: null,
  selectedYear: currentYear,
};

export const fetchPublicHolidaysThunk = createAsyncThunk(
  'publicHolidays/fetchAll',
  (year: number) => publicHolidayApi.fetchPublicHolidays(year)
);

export const createPublicHolidayThunk = createAsyncThunk(
  'publicHolidays/create',
  (dto: CreatePublicHolidayDto) => publicHolidayApi.createPublicHoliday(dto)
);

export const updatePublicHolidayThunk = createAsyncThunk(
  'publicHolidays/update',
  ({ id, dto }: { id: string; dto: UpdatePublicHolidayDto }) => publicHolidayApi.updatePublicHoliday(id, dto)
);

export const deletePublicHolidayThunk = createAsyncThunk(
  'publicHolidays/delete',
  (id: string) => publicHolidayApi.deletePublicHoliday(id)
);

const publicHolidaySlice = createSlice({
  name: 'publicHolidays',
  initialState,
  reducers: {
    setSelectedYear(state, action) {
      state.selectedYear = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchPublicHolidaysThunk.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchPublicHolidaysThunk.fulfilled, (state, action) => {
        state.loading = false;
        state.holidays = action.payload;
      })
      .addCase(fetchPublicHolidaysThunk.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message ?? 'Failed to fetch holidays';
      })
      .addCase(createPublicHolidayThunk.fulfilled, (state, action) => {
        state.holidays.push(action.payload);
      })
      .addCase(updatePublicHolidayThunk.fulfilled, (state, action) => {
        const idx = state.holidays.findIndex((h) => h.id === action.payload.id);
        if (idx !== -1) state.holidays[idx] = action.payload;
      })
      .addCase(deletePublicHolidayThunk.fulfilled, (state, action) => {
        const id = action.meta.arg;
        state.holidays = state.holidays.filter((h) => h.id !== id);
      });
  },
});

export const { setSelectedYear } = publicHolidaySlice.actions;
export default publicHolidaySlice.reducer;
