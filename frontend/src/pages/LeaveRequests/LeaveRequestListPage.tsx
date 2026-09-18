import React, { useEffect, useState, useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box, Button, CircularProgress, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, Typography,
  Paper, Chip, Alert, Tabs, Tab, IconButton, Tooltip,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import BlockIcon from '@mui/icons-material/Block';
import {
  fetchLeaveRequestsThunk,
  approveLeaveRequestThunk,
  rejectLeaveRequestThunk,
  cancelLeaveRequestThunk,
} from '../../store/leaveRequestSlice';
import type { AppDispatch, RootState } from '../../store/store';
import type { LeaveRequestDto } from '../../api/leaveRequestApi';
import LeaveRequestFormDialog from './LeaveRequestFormDialog';

type TabValue = 'All' | 'Pending' | 'Approved' | 'Rejected';

const statusColor = (status: string): 'default' | 'warning' | 'success' | 'error' | 'info' => {
  switch (status) {
    case 'Pending': return 'warning';
    case 'Approved': return 'success';
    case 'Rejected': return 'error';
    case 'Cancelled': return 'default';
    case 'Draft': return 'info';
    default: return 'default';
  }
};

const LeaveRequestListPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { requests, loading, error } = useSelector((state: RootState) => state.leaveRequests);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [activeTab, setActiveTab] = useState<TabValue>('All');
  const [rejectingId, setRejectingId] = useState<string | null>(null);

  const loadRequests = useCallback(() => {
    dispatch(fetchLeaveRequestsThunk());
  }, [dispatch]);

  useEffect(() => {
    loadRequests();
  }, [loadRequests]);

  const handleDialogClose = (saved: boolean) => {
    setDialogOpen(false);
    if (saved) loadRequests();
  };

  const handleApprove = (id: string) => {
    dispatch(approveLeaveRequestThunk(id));
  };

  const handleReject = (id: string) => {
    const reason = window.prompt('Rejection reason (required):');
    if (reason && reason.trim()) {
      setRejectingId(id);
      dispatch(rejectLeaveRequestThunk({ id, reason: reason.trim() })).finally(() => setRejectingId(null));
    }
  };

  const handleCancel = (id: string) => {
    if (window.confirm('Cancel this leave request?')) {
      dispatch(cancelLeaveRequestThunk(id));
    }
  };

  const filtered: LeaveRequestDto[] =
    activeTab === 'All' ? requests : requests.filter((r) => r.status === activeTab);

  const showSpinner = loading && requests.length === 0;

  return (
    <div data-testid="leave-request-list">
      <Box sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h5">Leave Requests</Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            data-testid="apply-leave-btn"
            onClick={() => setDialogOpen(true)}
          >
            Apply for Leave
          </Button>
        </Box>

        <Tabs
          value={activeTab}
          onChange={(_, val) => setActiveTab(val as TabValue)}
          sx={{ mb: 2 }}
        >
          <Tab label="All" value="All" />
          <Tab label="Pending" value="Pending" />
          <Tab label="Approved" value="Approved" />
          <Tab label="Rejected" value="Rejected" />
        </Tabs>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {showSpinner ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
            <CircularProgress data-testid="loading-spinner" />
          </Box>
        ) : filtered.length === 0 ? (
          <Box sx={{ textAlign: 'center', mt: 4 }} data-testid="empty-leave-requests">
            <Typography color="text.secondary">No leave requests found</Typography>
          </Box>
        ) : (
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Employee</TableCell>
                  <TableCell>Leave Type</TableCell>
                  <TableCell>Start Date</TableCell>
                  <TableCell>End Date</TableCell>
                  <TableCell>Days</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Reason</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {filtered.map((req) => (
                  <TableRow key={req.id} data-testid={`leave-request-row-${req.id}`}>
                    <TableCell>{req.employeeName}</TableCell>
                    <TableCell>{req.leaveTypeName}</TableCell>
                    <TableCell>{req.startDate}</TableCell>
                    <TableCell>{req.endDate}</TableCell>
                    <TableCell>{req.totalDays}</TableCell>
                    <TableCell>
                      <Chip
                        label={req.status}
                        color={statusColor(req.status)}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>{req.reason}</TableCell>
                    <TableCell>
                      {req.status === 'Pending' && (
                        <>
                          <Tooltip title="Approve">
                            <IconButton
                              size="small"
                              color="success"
                              data-testid={`approve-btn-${req.id}`}
                              onClick={() => handleApprove(req.id)}
                            >
                              <CheckCircleIcon />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title="Reject">
                            <IconButton
                              size="small"
                              color="error"
                              data-testid={`reject-btn-${req.id}`}
                              onClick={() => handleReject(req.id)}
                              disabled={rejectingId === req.id}
                            >
                              <BlockIcon />
                            </IconButton>
                          </Tooltip>
                          <Tooltip title="Cancel">
                            <IconButton
                              size="small"
                              data-testid={`cancel-btn-${req.id}`}
                              onClick={() => handleCancel(req.id)}
                            >
                              <CancelIcon />
                            </IconButton>
                          </Tooltip>
                        </>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        )}

        <LeaveRequestFormDialog open={dialogOpen} onClose={handleDialogClose} />
      </Box>
    </div>
  );
};

export default LeaveRequestListPage;
