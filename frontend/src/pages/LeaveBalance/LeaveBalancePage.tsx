import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
  Paper,
  Chip,
} from '@mui/material';
import TuneIcon from '@mui/icons-material/Tune';
import {
  fetchMyLeaveBalancesThunk,
  adjustLeaveBalanceThunk,
  clearError,
} from '../../store/leaveBalanceSlice';
import type { AppDispatch, RootState } from '../../store/store';
import type { LeaveBalanceDto } from '../../api/leaveBalanceApi';

const LeaveBalancePage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { balances, loading, error, adjusting } = useSelector(
    (state: RootState) => state.leaveBalance
  );
  const userRoles: string[] = useSelector(
    (state: RootState) => (state.auth as any)?.user?.roles ?? []
  );
  const isHRAdmin = userRoles.includes('HRAdmin') || userRoles.includes('SuperAdmin');

  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedBalance, setSelectedBalance] = useState<LeaveBalanceDto | null>(null);
  const [adjustment, setAdjustment] = useState('');
  const [reason, setReason] = useState('');
  const [successMsg, setSuccessMsg] = useState('');

  useEffect(() => {
    dispatch(fetchMyLeaveBalancesThunk());
  }, [dispatch]);

  const handleOpenAdjust = (balance: LeaveBalanceDto) => {
    setSelectedBalance(balance);
    setAdjustment('');
    setReason('');
    dispatch(clearError());
    setDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
    setSelectedBalance(null);
  };

  const handleAdjust = async () => {
    if (!selectedBalance) return;
    const adj = parseFloat(adjustment);
    if (isNaN(adj)) return;

    const result = await dispatch(
      adjustLeaveBalanceThunk({
        employeeId: selectedBalance.employeeId,
        leaveTypeId: selectedBalance.leaveTypeId,
        year: selectedBalance.year,
        adjustment: adj,
        reason,
      })
    );
    if (adjustLeaveBalanceThunk.fulfilled.match(result)) {
      setSuccessMsg(`Balance for ${selectedBalance.leaveTypeName} adjusted successfully.`);
      setDialogOpen(false);
      dispatch(fetchMyLeaveBalancesThunk());
    }
  };

  return (
    <div data-testid="leave-balance-page">
      <Box sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h5">Leave Balances</Typography>
        </Box>

        {successMsg && (
          <Alert severity="success" onClose={() => setSuccessMsg('')} sx={{ mb: 2 }}>
            {successMsg}
          </Alert>
        )}

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {loading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
            <CircularProgress data-testid="loading-spinner" />
          </Box>
        ) : balances.length === 0 ? (
          <Box sx={{ textAlign: 'center', mt: 4 }} data-testid="empty-balances">
            <Typography color="text.secondary">No leave balances found.</Typography>
          </Box>
        ) : (
          <TableContainer component={Paper} data-testid="leave-balance-table">
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Leave Type</TableCell>
                  <TableCell align="right">Total Days</TableCell>
                  <TableCell align="right">Used</TableCell>
                  <TableCell align="right">Pending</TableCell>
                  <TableCell align="right">Adjusted</TableCell>
                  <TableCell align="right">Available</TableCell>
                  <TableCell align="center">Year</TableCell>
                  {isHRAdmin && <TableCell align="center">Actions</TableCell>}
                </TableRow>
              </TableHead>
              <TableBody>
                {balances.map((balance) => (
                  <TableRow
                    key={balance.id}
                    data-testid={`balance-row-${balance.leaveTypeId}`}
                  >
                    <TableCell>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Chip label={balance.leaveTypeCode} size="small" variant="outlined" />
                        {balance.leaveTypeName}
                      </Box>
                    </TableCell>
                    <TableCell align="right">{balance.totalDays}</TableCell>
                    <TableCell align="right">{balance.usedDays}</TableCell>
                    <TableCell align="right">{balance.pendingDays}</TableCell>
                    <TableCell align="right">
                      {balance.adjustedDays > 0 ? `+${balance.adjustedDays}` : balance.adjustedDays}
                    </TableCell>
                    <TableCell align="right">
                      <Typography
                        component="span"
                        color={balance.availableDays <= 0 ? 'error' : 'success.main'}
                        sx={{ fontWeight: 600 }}
                      >
                        {balance.availableDays}
                      </Typography>
                    </TableCell>
                    <TableCell align="center">{balance.year}</TableCell>
                    {isHRAdmin && (
                      <TableCell align="center">
                        <Button
                          size="small"
                          variant="outlined"
                          startIcon={<TuneIcon />}
                          data-testid="adjust-balance-btn"
                          onClick={() => handleOpenAdjust(balance)}
                        >
                          Adjust
                        </Button>
                      </TableCell>
                    )}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}

        <Dialog
          open={dialogOpen}
          onClose={handleCloseDialog}
          maxWidth="xs"
          fullWidth
          data-testid="adjust-dialog"
        >
          <DialogTitle>Adjust Leave Balance</DialogTitle>
          <DialogContent>
            {selectedBalance && (
              <Box sx={{ pt: 1, display: 'flex', flexDirection: 'column', gap: 2 }}>
                <Typography variant="body2" color="text.secondary">
                  Adjusting: <strong>{selectedBalance.leaveTypeName}</strong> ({selectedBalance.year})
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Current available: <strong>{selectedBalance.availableDays} days</strong>
                </Typography>
                <TextField
                  label="Adjustment (days)"
                  type="number"
                  value={adjustment}
                  onChange={(e) => setAdjustment(e.target.value)}
                  helperText="Use positive to add, negative to deduct"
                  fullWidth
                  size="small"
                  slotProps={{ htmlInput: { 'data-testid': 'adjustment-input' } }}
                />
                <TextField
                  label="Reason"
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  fullWidth
                  multiline
                  rows={3}
                  size="small"
                  slotProps={{ htmlInput: { 'data-testid': 'reason-input' } }}
                />
                {error && (
                  <Alert severity="error" sx={{ mt: 1 }}>
                    {error}
                  </Alert>
                )}
              </Box>
            )}
          </DialogContent>
          <DialogActions>
            <Button onClick={handleCloseDialog} data-testid="cancel-adjust-btn">Cancel</Button>
            <Button
              variant="contained"
              onClick={handleAdjust}
              disabled={adjusting || !adjustment || !reason}
              data-testid="submit-adjust-btn"
            >
              {adjusting ? <CircularProgress size={20} /> : 'Apply Adjustment'}
            </Button>
          </DialogActions>
        </Dialog>
      </Box>
    </div>
  );
};

export default LeaveBalancePage;
