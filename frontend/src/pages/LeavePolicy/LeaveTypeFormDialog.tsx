import React, { useState } from 'react';
import {
  Button, Checkbox, Dialog, DialogActions, DialogContent,
  DialogTitle, FormControlLabel, TextField,
} from '@mui/material';
import { useDispatch } from 'react-redux';
import type { AppDispatch } from '../../store/store';
import { createLeaveTypeThunk } from '../../store/leavePolicySlice';
import type { CreateLeaveTypeDto } from '../../types';

interface Props {
  open: boolean;
  onClose: () => void;
}

const LeaveTypeFormDialog: React.FC<Props> = ({ open, onClose }) => {
  const dispatch = useDispatch<AppDispatch>();
  const [name, setName] = useState('');
  const [code, setCode] = useState('');
  const [annualDays, setAnnualDays] = useState<number>(0);
  const [requiresAttachment, setRequiresAttachment] = useState(false);
  const [requiresHrApproval, setRequiresHrApproval] = useState(false);

  const handleSubmit = () => {
    const dto: CreateLeaveTypeDto = {
      name,
      code: code.slice(0, 5),
      annualDays,
      requiresAttachment,
      requiresHrApproval,
    };
    dispatch(createLeaveTypeThunk(dto));
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} data-testid="leave-type-dialog">
      <DialogTitle>Add Leave Type</DialogTitle>
      <DialogContent>
        <div data-testid="lt-name-input">
          <TextField
            label="Name"
            required
            fullWidth
            margin="normal"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </div>
        <div data-testid="lt-code-input">
          <TextField
            label="Code"
            required
            fullWidth
            margin="normal"
            value={code}
            onChange={(e) => setCode(e.target.value.slice(0, 5))}
          />
        </div>
        <div data-testid="lt-annual-days-input">
          <TextField
            label="Annual Days"
            type="number"
            fullWidth
            margin="normal"
            value={annualDays}
            onChange={(e) => setAnnualDays(Number(e.target.value))}
          />
        </div>
        <FormControlLabel
          control={
            <Checkbox
              checked={requiresAttachment}
              onChange={(e) => setRequiresAttachment(e.target.checked)}
            />
          }
          label="Requires Attachment"
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={requiresHrApproval}
              onChange={(e) => setRequiresHrApproval(e.target.checked)}
            />
          }
          label="Requires HR Approval"
        />
      </DialogContent>
      <DialogActions>
        <Button data-testid="lt-cancel-btn" onClick={onClose}>Cancel</Button>
        <Button
          data-testid="lt-submit-btn"
          variant="contained"
          onClick={handleSubmit}
          disabled={!name || !code}
        >
          Submit
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default LeaveTypeFormDialog;
