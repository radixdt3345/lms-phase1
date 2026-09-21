import React, { useState, useEffect } from 'react';
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, TextField, Alert, CircularProgress,
} from '@mui/material';
import { useDispatch, useSelector } from 'react-redux';
import { createCompOffRequestThunk } from '../../store/compOffSlice';
import type { AppDispatch, RootState } from '../../store/store';

interface Props {
  open: boolean;
  onClose: (saved: boolean) => void;
}

const CompOffFormDialog: React.FC<Props> = ({ open, onClose }) => {
  const dispatch = useDispatch<AppDispatch>();
  const { submitting, error } = useSelector((state: RootState) => state.compOff);

  const [workedDate, setWorkedDate] = useState('');
  const [hoursWorked, setHoursWorked] = useState('');
  const [description, setDescription] = useState('');
  const [validationError, setValidationError] = useState('');

  useEffect(() => {
    if (open) {
      setWorkedDate('');
      setHoursWorked('');
      setDescription('');
      setValidationError('');
    }
  }, [open]);

  const handleSubmit = async () => {
    const hours = parseFloat(hoursWorked);
    if (!workedDate) { setValidationError('Please select the worked date.'); return; }
    if (!hoursWorked || isNaN(hours) || hours < 4) {
      setValidationError('Hours worked must be at least 4.');
      return;
    }
    if (hours > 24) { setValidationError('Hours worked cannot exceed 24.'); return; }
    if (!description.trim()) { setValidationError('Please provide a description.'); return; }
    setValidationError('');

    const result = await dispatch(createCompOffRequestThunk({
      workedDate,
      hoursWorked: hours,
      description: description.trim(),
    }));
    if (createCompOffRequestThunk.fulfilled.match(result)) {
      onClose(true);
    }
  };

  return (
    <Dialog open={open} onClose={() => onClose(false)} maxWidth="sm" fullWidth>
      <DialogTitle>Apply for Comp-Off</DialogTitle>
      <DialogContent data-testid="comp-off-form">
        {(validationError || error) && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {validationError || error}
          </Alert>
        )}
        <TextField
          fullWidth
          label="Worked Date"
          type="date"
          value={workedDate}
          onChange={(e) => setWorkedDate(e.target.value)}
          margin="normal"
          slotProps={{ inputLabel: { shrink: true }, htmlInput: { 'data-testid': 'worked-date-input' } }}
        />
        <TextField
          fullWidth
          label="Hours Worked"
          type="number"
          value={hoursWorked}
          onChange={(e) => setHoursWorked(e.target.value)}
          margin="normal"
          slotProps={{ htmlInput: { 'data-testid': 'hours-input', min: 4, max: 24, step: 0.5 } }}
          helperText="Minimum 4 hours. 4-7.5 hours = 0.5 day credit; 8+ hours = 1 day credit."
        />
        <TextField
          fullWidth
          label="Description"
          multiline
          rows={3}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          margin="normal"
          slotProps={{ htmlInput: { 'data-testid': 'description-input' } }}
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

export default CompOffFormDialog;
