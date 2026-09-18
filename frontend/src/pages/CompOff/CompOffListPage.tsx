import React, { useEffect, useState, useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box, Button, Card, CardContent, CircularProgress, Grid,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Typography, Paper, Chip, Alert, IconButton, Tooltip,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import BlockIcon from '@mui/icons-material/Block';
import {
  fetchCompOffRequestsThunk,
  approveCompOffRequestThunk,
  rejectCompOffRequestThunk,
  fetchCompOffCreditsThunk,
} from '../../store/compOffSlice';
import type { AppDispatch, RootState } from '../../store/store';
import CompOffFormDialog from './CompOffFormDialog';

const statusColor = (status: string): 'default' | 'warning' | 'success' | 'error' => {
  switch (status) {
    case 'Pending': return 'warning';
    case 'Approved': return 'success';
    case 'Rejected': return 'error';
    default: return 'default';
  }
};

const CompOffListPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { requests, credits, loading, error } = useSelector((state: RootState) => state.compOff);
  const [dialogOpen, setDialogOpen] = useState(false);

  const loadData = useCallback(() => {
    dispatch(fetchCompOffRequestsThunk());
    dispatch(fetchCompOffCreditsThunk());
  }, [dispatch]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleDialogClose = (saved: boolean) => {
    setDialogOpen(false);
    if (saved) loadData();
  };

  const handleApprove = (id: string) => {
    dispatch(approveCompOffRequestThunk(id));
  };

  const handleReject = (id: string) => {
    const reason = window.prompt('Rejection reason (required):');
    if (reason && reason.trim()) {
      dispatch(rejectCompOffRequestThunk({ id, reason: reason.trim() }));
    }
  };

  const showSpinner = loading && requests.length === 0;

  return (
    <div data-testid="comp-off-list">
      <Box sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
          <Typography variant="h5">Comp-Off Requests</Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            data-testid="apply-comp-off-btn"
            onClick={() => setDialogOpen(true)}
          >
            Apply for Comp-Off
          </Button>
        </Box>

        {/* Credits Summary Card */}
        {credits && (
          <Card data-testid="credits-summary" sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Comp-Off Credits Balance
              </Typography>
              <Grid container spacing={3}>
                <Grid item xs={12} sm={3}>
                  <Typography variant="body2" color="text.secondary">Total Earned</Typography>
                  <Typography variant="h4">{credits.totalCredits}</Typography>
                </Grid>
                <Grid item xs={12} sm={3}>
                  <Typography variant="body2" color="text.secondary">Used</Typography>
                  <Typography variant="h4">{credits.usedCredits}</Typography>
                </Grid>
                <Grid item xs={12} sm={3}>
                  <Typography variant="body2" color="text.secondary">Available</Typography>
                  <Typography variant="h4" color="success.main">{credits.availableCredits}</Typography>
                </Grid>
                <Grid item xs={12} sm={3}>
                  <Typography variant="body2" color="text.secondary">Expiring Soon</Typography>
                  <Typography variant="h4" color="warning.main">{credits.expiringCredits}</Typography>
                  {credits.expiryDate && (
                    <Typography variant="caption" color="text.secondary">
                      by {credits.expiryDate}
                    </Typography>
                  )}
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        )}

        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {showSpinner ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
            <CircularProgress data-testid="loading-spinner" />
          </Box>
        ) : requests.length === 0 ? (
          <Box sx={{ textAlign: 'center', mt: 4 }} data-testid="empty-comp-off">
            <Typography color="text.secondary">No comp-off requests found</Typography>
          </Box>
        ) : (
          <TableContainer component={Paper}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Employee</TableCell>
                  <TableCell>Worked Date</TableCell>
                  <TableCell>Hours</TableCell>
                  <TableCell>Credit (Days)</TableCell>
                  <TableCell>Description</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {requests.map((req) => (
                  <TableRow key={req.id} data-testid={`comp-off-row-${req.id}`}>
                    <TableCell>{req.employeeName}</TableCell>
                    <TableCell>{req.workedDate}</TableCell>
                    <TableCell>{req.hoursWorked}</TableCell>
                    <TableCell>{req.creditDays}</TableCell>
                    <TableCell>{req.description}</TableCell>
                    <TableCell>
                      <Chip label={req.status} color={statusColor(req.status)} size="small" />
                    </TableCell>
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
                            >
                              <BlockIcon />
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

        <CompOffFormDialog open={dialogOpen} onClose={handleDialogClose} />
      </Box>
    </div>
  );
};

export default CompOffListPage;
