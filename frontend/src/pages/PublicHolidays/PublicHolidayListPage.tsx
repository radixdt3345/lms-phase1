import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box, Button, CircularProgress, IconButton, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, Typography, Paper, Chip
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import {
  fetchPublicHolidaysThunk,
  deletePublicHolidayThunk,
  setSelectedYear,
} from '../../store/publicHolidaySlice';
import type { AppDispatch, RootState } from '../../store/store';
import type { PublicHolidayDto } from '../../api/publicHolidayApi';
import PublicHolidayFormDialog from './PublicHolidayFormDialog';

const PublicHolidayListPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { holidays, loading, error, selectedYear } = useSelector(
    (state: RootState) => state.publicHolidays
  );
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedHoliday, setSelectedHoliday] = useState<PublicHolidayDto | undefined>(undefined);

  useEffect(() => {
    dispatch(fetchPublicHolidaysThunk(selectedYear));
  }, [dispatch, selectedYear]);

  const handleAdd = () => {
    setSelectedHoliday(undefined);
    setDialogOpen(true);
  };

  const handleEdit = (holiday: PublicHolidayDto) => {
    setSelectedHoliday(holiday);
    setDialogOpen(true);
  };

  const handleDelete = (holiday: PublicHolidayDto) => {
    if (window.confirm(`Delete holiday "${holiday.name}"?`)) {
      dispatch(deletePublicHolidayThunk(holiday.id));
    }
  };

  const handlePrevYear = () => {
    dispatch(setSelectedYear(selectedYear - 1));
  };

  const handleNextYear = () => {
    dispatch(setSelectedYear(selectedYear + 1));
  };

  const showSpinner = loading && holidays.length === 0;

  return (
    <div data-testid="public-holiday-page">
      <Box sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h5">Public Holidays</Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            data-testid="add-holiday-btn"
            onClick={handleAdd}
          >
            Add Holiday
          </Button>
        </Box>

        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
          <IconButton data-testid="prev-year-btn" onClick={handlePrevYear} size="small">
            <ChevronLeftIcon />
          </IconButton>
          <Typography variant="h6" data-testid="year-selector">
            {selectedYear}
          </Typography>
          <IconButton data-testid="next-year-btn" onClick={handleNextYear} size="small">
            <ChevronRightIcon />
          </IconButton>
        </Box>

        {error && (
          <Typography color="error" data-testid="error-message" sx={{ mb: 2 }}>
            {error}
          </Typography>
        )}

        {showSpinner ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
            <CircularProgress data-testid="loading-spinner" />
          </Box>
        ) : (
          <TableContainer component={Paper} data-testid="holidays-table">
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>Date</TableCell>
                  <TableCell>Country Code</TableCell>
                  <TableCell>Optional</TableCell>
                  <TableCell>Active</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {holidays.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} align="center" data-testid="empty-holidays">
                      No public holidays found for {selectedYear}
                    </TableCell>
                  </TableRow>
                ) : (
                  holidays.map((holiday) => (
                    <TableRow key={holiday.id} data-testid={`holiday-row-${holiday.id}`}>
                      <TableCell>{holiday.name}</TableCell>
                      <TableCell>{holiday.date}</TableCell>
                      <TableCell>{holiday.countryCode}</TableCell>
                      <TableCell>
                        <Chip
                          label={holiday.isOptional ? 'Optional' : 'Mandatory'}
                          color={holiday.isOptional ? 'warning' : 'success'}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={holiday.isActive ? 'Active' : 'Inactive'}
                          color={holiday.isActive ? 'success' : 'default'}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <IconButton
                          size="small"
                          data-testid={`edit-holiday-${holiday.id}`}
                          onClick={() => handleEdit(holiday)}
                        >
                          <EditIcon />
                        </IconButton>
                        <IconButton
                          size="small"
                          color="error"
                          data-testid={`delete-holiday-${holiday.id}`}
                          onClick={() => handleDelete(holiday)}
                        >
                          <DeleteIcon />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}

        <PublicHolidayFormDialog
          open={dialogOpen}
          onClose={() => setDialogOpen(false)}
          holiday={selectedHoliday}
          year={selectedYear}
        />
      </Box>
    </div>
  );
};

export default PublicHolidayListPage;
