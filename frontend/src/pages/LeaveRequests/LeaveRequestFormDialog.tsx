import React, { useState, useEffect } from 'react';
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, TextField, MenuItem, Alert, CircularProgress,
} from '@mui/material';
import { useDispatch, useSelector } from 'react-redux';
import { createLeaveRequestThunk } from '../../store/leaveRequestSlice';
import type { AppDispatch, RootState } from '../../store/store';
import { fetchLeaveTypesThunk } from '../../store/leavePolicySlice';

interface Props {
  open: boolean;
  onClose: (saved: boolean) => void;
}

const LeaveRequestFormDialog: React.FC<Props> = ({ open, onClose }) => {
  const dispatch = useDispatch<AppDispatch>();
  const { submitting, error } = useSelector((state: RootState) => state.leaveRequests);
  const { leaveTypes } = useSelector((state: RootState) => state.leavePolicy);

  const [leaveTypeId, setLeaveTypeId] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [reason, setReason] = useState('');
  const [validationError, setValidationError] = useState('');

  useEffect(() => {
    if (open) {
      dispatch(fetchLeaveTypesThunk());
      setLeaveTypeId('');
      setStartDate('');
      setEndDate('');
      setReason('');
      setValidationError('');
    }
  }, [open, dispatch]);

  const handleSubmit = async () => {
    if (!leaveTypeId) { setValidationError('Please select a leave type.'); return; }
    if (!startDate) { setValidationError('Please enter a start date.'); return; }
    if (!endDate) { setValidationError('Please enter an end date.'); return; }
    if (endDate < startDate) { setValidationError('End date must be on or after start date.'); return; }
    if (!reason.trim()) { setValidationError('Please provide a reason.'); return; }
    setValidationError('');

    const result = await dispatch(createLeaveRequestThunk({ leaveTypeId, startDate, endDate, reason }));
    if (createLeaveRequestThunk.fulfilled.match(result)) {
      onClose(true);
    }
  };

  return (
    <Dialog open={open} onClose={() => onClose(false)} maxWidth="sm" fullWidth>
      <DialogTitle>Apply for Leave</DialogTitle>
      <DialogContent data-testid="leave-request-form">
        {(validationError || error) && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {validationError || error}
          </Alert>
        )}
        <TextField
          select
          fullWidth
          label="Leave Type"
          value={leaveTypeId}
          onChange={(e) => setLeaveTypeId(e.target.value)}
          margin="normal"
          inputProps={{ 'data-testid': 'leave-type-select' }}
        >
          {leaveTypes.map((lt) => (
            <MenuItem key={lt.id} value={lt.id}>
              {lt.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          fullWidth
          label="Start Date"
          type="date"
          value={startDate}
          onChange={(e) => setStartDate(e.target.value)}
          margin="normal"
          InputLabelProps={{ shrink: true }}
          inputProps={{ 'data-testid': 'start-date-input' }}
        />
        <TextField
          fullWidth
          label="End Date"
          type="date"
          value={endDate}
          onChange={(e) => setEndDate(e.target.value)}
          margin="normal"
          InputLabelProps={{ shrink: true }}
          inputProps={{ 'data-testid': 'end-date-input' }}
        />
        <TextField
          fullWidth
          label="Reason"
          multiline
          rows={3}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          margin="normal"
          inputProps={{ 'data-testid': 'reason-input' }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={() => onClose(false)} disabled={submitting}>
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={handleSubmit}
          disabled={submitting}
          data-testid="submit-btn"
          startIcon={submitting ? <CircularProgress size={16} /> : null}
        >
          Submit
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default LeaveRequestFormDialog;
