import React, { useEffect, useState } from 'react';
import {
  Alert, Box, Button, CircularProgress, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, Paper, Typography,
} from '@mui/material';
import { useDispatch, useSelector } from 'react-redux';
import type { AppDispatch, RootState } from '../../store/store';
import { fetchLeaveTypesThunk } from '../../store/leavePolicySlice';
import LeaveTypeFormDialog from './LeaveTypeFormDialog';
import LeavePoliciesPanel from './LeavePoliciesPanel';

const LeaveTypeListPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { leaveTypes, loading, error } = useSelector((s: RootState) => s.leavePolicy);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedLeaveTypeId, setSelectedLeaveTypeId] = useState<string | null>(null);

  useEffect(() => {
    dispatch(fetchLeaveTypesThunk());
  }, [dispatch]);

  // Show spinner only when loading and no cached data available yet
  const showSpinner = loading && leaveTypes.length === 0;

  return (
    <div data-testid="leave-type-list-page">
      <Box sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h5">Leave Types</Typography>
          <Button
            variant="contained"
            data-testid="add-leave-type-btn"
            onClick={() => setDialogOpen(true)}
          >
            Add Leave Type
          </Button>
        </Box>

        {showSpinner && <CircularProgress data-testid="loading-spinner" />}

        {error && (
          <Alert severity="error" data-testid="leave-types-error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {!showSpinner && !error && leaveTypes.length === 0 && (
          <Typography color="text.secondary" data-testid="no-leave-types-msg">No leave types found.</Typography>
        )}

        {leaveTypes.length > 0 && (
          <TableContainer component={Paper} data-testid="leave-types-table">
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Code</TableCell>
                  <TableCell>Name</TableCell>
                  <TableCell>Annual Days</TableCell>
                  <TableCell>Attachment Required</TableCell>
                  <TableCell>HR Approval Required</TableCell>
                  <TableCell>Active</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {leaveTypes.map((lt) => (
                  <TableRow key={lt.id} data-testid={`leave-type-row-${lt.id}`}>
                    <TableCell>{lt.code}</TableCell>
                    <TableCell>{lt.name}</TableCell>
                    <TableCell>{lt.annualDays}</TableCell>
                    <TableCell>{lt.requiresAttachment ? 'Yes' : 'No'}</TableCell>
                    <TableCell>{lt.requiresHrApproval ? 'Yes' : 'No'}</TableCell>
                    <TableCell>{lt.isActive ? 'Yes' : 'No'}</TableCell>
                    <TableCell>
                      <Button
                        size="small"
                        variant="outlined"
                        data-testid={`manage-policies-${lt.id}`}
                        onClick={() => setSelectedLeaveTypeId(lt.id)}
                      >
                        Manage Policies
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}

        {selectedLeaveTypeId && (
          <Box sx={{ mt: 3 }}>
            <LeavePoliciesPanel leaveTypeId={selectedLeaveTypeId} />
          </Box>
        )}

        <LeaveTypeFormDialog open={dialogOpen} onClose={() => setDialogOpen(false)} />
      </Box>
    </div>
  );
};

export default LeaveTypeListPage;
