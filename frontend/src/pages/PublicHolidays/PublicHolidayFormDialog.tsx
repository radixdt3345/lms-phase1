import React, { useState, useEffect } from 'react';
import { useDispatch } from 'react-redux';
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, TextField, Box, FormControlLabel, Checkbox
} from '@mui/material';
import { createPublicHolidayThunk, updatePublicHolidayThunk } from '../../store/publicHolidaySlice';
import type { AppDispatch } from '../../store/store';
import type { PublicHolidayDto } from '../../api/publicHolidayApi';

interface Props {
  open: boolean;
  onClose: () => void;
  holiday?: PublicHolidayDto;
  year: number;
}

const PublicHolidayFormDialog: React.FC<Props> = ({ open, onClose, holiday, year }) => {
  const dispatch = useDispatch<AppDispatch>();
  const [name, setName] = useState('');
  const [date, setDate] = useState('');
  const [country, setCountry] = useState('');
  const [region, setRegion] = useState('');
  const [isRecurring, setIsRecurring] = useState(false);
  const [description, setDescription] = useState('');

  useEffect(() => {
    if (holiday) {
      setName(holiday.name);
      setDate(holiday.date);
      setCountry(holiday.country);
      setRegion(holiday.region ?? '');
      setIsRecurring(holiday.isRecurring);
      setDescription(holiday.description ?? '');
    } else {
      setName('');
      setDate(`${year}-01-01`);
      setCountry('');
      setRegion('');
      setIsRecurring(false);
      setDescription('');
    }
  }, [holiday, open, year]);

  const handleSubmit = () => {
    if (!name || !date || !country) return;
    const dto = {
      name,
      date,
      country,
      region: region || undefined,
      isRecurring,
      description: description || undefined,
    };
    if (holiday) {
      dispatch(updatePublicHolidayThunk({ id: holiday.id, dto }));
    } else {
      dispatch(createPublicHolidayThunk(dto));
    }
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} data-testid="holiday-form-dialog" maxWidth="sm" fullWidth>
      <DialogTitle>{holiday ? 'Edit Public Holiday' : 'Add Public Holiday'}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <TextField
            label="Name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'holiday-name-input' } }}
          />
          <TextField
            label="Date"
            type="date"
            value={date}
            onChange={(e) => setDate(e.target.value)}
            required
            fullWidth
            slotProps={{
              htmlInput: { 'data-testid': 'holiday-date-input' },
              inputLabel: { shrink: true },
            }}
          />
          <TextField
            label="Country"
            value={country}
            onChange={(e) => setCountry(e.target.value)}
            required
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'holiday-country-input' } }}
          />
          <TextField
            label="Region (optional)"
            value={region}
            onChange={(e) => setRegion(e.target.value)}
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'holiday-region-input' } }}
          />
          <TextField
            label="Description (optional)"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            fullWidth
            multiline
            rows={2}
            slotProps={{ htmlInput: { 'data-testid': 'holiday-description-input' } }}
          />
          <Box data-testid="holiday-recurring-checkbox">
            <FormControlLabel
              control={
                <Checkbox
                  checked={isRecurring}
                  onChange={(e) => setIsRecurring(e.target.checked)}
                />
              }
              label="Recurring annually"
            />
          </Box>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} data-testid="holiday-cancel-btn">Cancel</Button>
        <Button onClick={handleSubmit} variant="contained" data-testid="holiday-submit-btn">
          {holiday ? 'Update' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default PublicHolidayFormDialog;
