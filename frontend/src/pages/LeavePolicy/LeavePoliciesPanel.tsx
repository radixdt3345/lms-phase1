import React, { useEffect, useState } from 'react';
import {
  Box, Button, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, Paper, Typography,
} from '@mui/material';
import { useDispatch, useSelector } from 'react-redux';
import type { AppDispatch, RootState } from '../../store/store';
import { fetchPoliciesThunk } from '../../store/leavePolicySlice';
import LeavePolicyFormDialog from './LeavePolicyFormDialog';

interface Props {
  leaveTypeId: string;
}

const LeavePoliciesPanel: React.FC<Props> = ({ leaveTypeId }) => {
  const dispatch = useDispatch<AppDispatch>();
  const { policies } = useSelector((s: RootState) => s.leavePolicy);
  const [dialogOpen, setDialogOpen] = useState(false);

  useEffect(() => {
    dispatch(fetchPoliciesThunk(leaveTypeId));
  }, [dispatch, leaveTypeId]);

  return (
    <div data-testid="policies-panel">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6">Policies</Typography>
        <Button
          variant="contained"
          size="small"
          data-testid="add-policy-btn"
          onClick={() => setDialogOpen(true)}
        >
          Add Policy
        </Button>
      </Box>

      {policies.length === 0 && (
        <Typography color="text.secondary">No policies found for this leave type.</Typography>
      )}

      {policies.length > 0 && (
        <TableContainer component={Paper}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Annual Allotment</TableCell>
                <TableCell>Max Carry Forward</TableCell>
                <TableCell>Max Consecutive Days</TableCell>
                <TableCell>Effective From</TableCell>
                <TableCell>Effective To</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {policies.map((p) => (
                <TableRow key={p.id} data-testid={`policy-row-${p.id}`}>
                  <TableCell>{p.annualAllotment}</TableCell>
                  <TableCell>{p.maxCarryForward}</TableCell>
                  <TableCell>{p.maxConsecutiveDays}</TableCell>
                  <TableCell>{p.effectiveFrom}</TableCell>
                  <TableCell>{p.effectiveTo ?? '—'}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <LeavePolicyFormDialog
        open={dialogOpen}
        leaveTypeId={leaveTypeId}
        onClose={() => setDialogOpen(false)}
      />
    </div>
  );
};

export default LeavePoliciesPanel;
