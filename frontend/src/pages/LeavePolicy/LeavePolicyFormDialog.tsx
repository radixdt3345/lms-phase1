import React, { useState } from 'react';
import {
  Button, Checkbox, Dialog, DialogActions, DialogContent,
  DialogTitle, FormControlLabel, TextField,
} from '@mui/material';
import { useDispatch } from 'react-redux';
import type { AppDispatch } from '../../store/store';
import type { CreateLeavePolicyDto } from '../../types';
import { createLeavePolicyThunk } from '../../store/leavePolicySlice';

interface Props {
  open: boolean;
  leaveTypeId: string;
  onClose: () => void;
}

const LeavePolicyFormDialog: React.FC<Props> = ({ open, leaveTypeId, onClose }) => {
  const dispatch = useDispatch<AppDispatch>();
  const [annualAllotment, setAnnualAllotment] = useState<number>(0);
  const [maxCarryForward, setMaxCarryForward] = useState<number>(0);
  const [maxConsecutiveDays, setMaxConsecutiveDays] = useState<number>(0);
  const [minNoticeDays, setMinNoticeDays] = useState<number>(0);
  const [accruedMonthly, setAccruedMonthly] = useState(false);
  const [accrualRate, setAccrualRate] = useState<number>(0);
  const [effectiveFrom, setEffectiveFrom] = useState('');
  const [effectiveTo, setEffectiveTo] = useState('');

  const handleSubmit = () => {
    const dto: CreateLeavePolicyDto = {
      leaveTypeId,
      annualAllotment,
      maxCarryForward,
      maxConsecutiveDays,
      minNoticeDays,
      accruedMonthly,
      accrualRate,
      effectiveFrom,
      effectiveTo: effectiveTo || undefined,
    };
    dispatch(createLeavePolicyThunk(dto));
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose}>
      <DialogTitle>Add Policy</DialogTitle>
      <DialogContent>
        <TextField
          label="Annual Allotment"
          type="number"
          fullWidth
          margin="normal"
          value={annualAllotment}
          onChange={(e) => setAnnualAllotment(Number(e.target.value))}
        />
        <TextField
          label="Max Carry Forward"
          type="number"
          fullWidth
          margin="normal"
          value={maxCarryForward}
          onChange={(e) => setMaxCarryForward(Number(e.target.value))}
        />
        <TextField
          label="Max Consecutive Days"
          type="number"
          fullWidth
          margin="normal"
          value={maxConsecutiveDays}
          onChange={(e) => setMaxConsecutiveDays(Number(e.target.value))}
        />
        <TextField
          label="Min Notice Days"
          type="number"
          fullWidth
          margin="normal"
          value={minNoticeDays}
          onChange={(e) => setMinNoticeDays(Number(e.target.value))}
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={accruedMonthly}
              onChange={(e) => setAccruedMonthly(e.target.checked)}
            />
          }
          label="Accrued Monthly"
        />
        <TextField
          label="Accrual Rate"
          type="number"
          fullWidth
          margin="normal"
          value={accrualRate}
          onChange={(e) => setAccrualRate(Number(e.target.value))}
        />
        <TextField
          label="Effective From"
          type="date"
          fullWidth
          margin="normal"
          slotProps={{ inputLabel: { shrink: true } }}
          value={effectiveFrom}
          onChange={(e) => setEffectiveFrom(e.target.value)}
        />
        <TextField
          label="Effective To"
          type="date"
          fullWidth
          margin="normal"
          slotProps={{ inputLabel: { shrink: true } }}
          value={effectiveTo}
          onChange={(e) => setEffectiveTo(e.target.value)}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={!effectiveFrom}>
          Submit
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default LeavePolicyFormDialog;
