import React, { useState, useEffect } from 'react';
import { useDispatch } from 'react-redux';
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, TextField, Box
} from '@mui/material';
import { createDepartmentThunk, updateDepartmentThunk } from '../../store/departmentSlice';
import type { AppDispatch } from '../../store/store';
import type { DepartmentDto } from '../../types';

interface Props {
  open: boolean;
  onClose: () => void;
  department?: DepartmentDto;
}

const DepartmentFormDialog: React.FC<Props> = ({ open, onClose, department }) => {
  const dispatch = useDispatch<AppDispatch>();
  const [name, setName] = useState('');
  const [code, setCode] = useState('');
  const [overlapLimit, setOverlapLimit] = useState(0);

  useEffect(() => {
    if (department) {
      setName(department.name);
      setCode(department.code);
      setOverlapLimit(department.overlapLimit);
    } else {
      setName('');
      setCode('');
      setOverlapLimit(0);
    }
  }, [department, open]);

  const handleSubmit = () => {
    if (!name || !code) return;
    if (department) {
      dispatch(updateDepartmentThunk({ id: department.id, dto: { name, code, overlapLimit } }));
    } else {
      dispatch(createDepartmentThunk({ name, code, overlapLimit }));
    }
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} data-testid="dept-form-dialog" maxWidth="sm" fullWidth>
      <DialogTitle>{department ? 'Edit Department' : 'Add Department'}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <TextField
            label="Name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'dept-name-input' } }}
          />
          <TextField
            label="Code"
            value={code}
            onChange={(e) => setCode(e.target.value)}
            required
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'dept-code-input', maxLength: 10 } }}
          />
          <TextField
            label="Overlap Limit"
            type="number"
            value={overlapLimit}
            onChange={(e) => setOverlapLimit(Number(e.target.value))}
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'dept-overlap-input' } }}
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} data-testid="dept-cancel-btn">Cancel</Button>
        <Button onClick={handleSubmit} variant="contained" data-testid="dept-submit-btn">
          {department ? 'Update' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default DepartmentFormDialog;
