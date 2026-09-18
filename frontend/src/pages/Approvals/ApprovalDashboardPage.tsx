import React, { useEffect, useState } from 'react';
import {
  Box,
  Typography,
  Tab,
  Tabs,
  Card,
  CardContent,
  Grid,
  Button,
  Chip,
  CircularProgress,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
} from '@mui/material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState, AppDispatch } from '../../store/store';
import {
  loadPendingApprovals,
  loadApprovalHistory,
  loadApprovalStats,
  escalateApprovalAsync,
} from '../../store/approvalSlice';

const ApprovalDashboardPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { pending, history, stats, loadingPending, loadingHistory, error } = useSelector(
    (s: RootState) => s.approvals,
  );

  const [activeTab, setActiveTab] = useState(0);
  const [escalateDialogOpen, setEscalateDialogOpen] = useState(false);
  const [escalateId, setEscalateId] = useState<string | null>(null);
  const [escalateReason, setEscalateReason] = useState('');

  useEffect(() => {
    dispatch(loadPendingApprovals());
    dispatch(loadApprovalStats());
    dispatch(loadApprovalHistory());
  }, [dispatch]);

  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const handleEscalateClick = (id: string) => {
    setEscalateId(id);
    setEscalateReason('');
    setEscalateDialogOpen(true);
  };

  const handleEscalateConfirm = () => {
    if (escalateId && escalateReason.trim()) {
      dispatch(escalateApprovalAsync({ id: escalateId, reason: escalateReason.trim() }));
    }
    setEscalateDialogOpen(false);
    setEscalateId(null);
  };

  const pendingColumns: GridColDef[] = [
    { field: 'employeeName', headerName: 'Employee', flex: 1 },
    { field: 'requestType', headerName: 'Type', width: 120 },
    { field: 'level', headerName: 'Level', width: 80 },
    {
      field: 'startDate',
      headerName: 'Start Date',
      width: 130,
      valueFormatter: (value) => value ? new Date(value as string).toLocaleDateString() : '—',
    },
    {
      field: 'endDate',
      headerName: 'End Date',
      width: 130,
      valueFormatter: (value) => value ? new Date(value as string).toLocaleDateString() : '—',
    },
    { field: 'requestedDays', headerName: 'Days', width: 80 },
    {
      field: 'submittedAt',
      headerName: 'Submitted',
      width: 160,
      valueFormatter: (value) => value ? new Date(value as string).toLocaleString() : '—',
    },
    {
      field: 'actions',
      headerName: 'Actions',
      width: 260,
      sortable: false,
      renderCell: (params) => (
        <Box sx={{ display: 'flex', gap: 1 }}>
          <Button
            size="small"
            variant="contained"
            color="success"
            data-testid={`approve-btn-${params.row.id}`}
          >
            Approve
          </Button>
          <Button
            size="small"
            variant="contained"
            color="error"
            data-testid={`reject-btn-${params.row.id}`}
          >
            Reject
          </Button>
          <Button
            size="small"
            variant="outlined"
            data-testid={`escalate-btn-${params.row.id}`}
            onClick={() => handleEscalateClick(params.row.id)}
          >
            Escalate
          </Button>
        </Box>
      ),
    },
  ];

  const historyColumns: GridColDef[] = [
    { field: 'approverId', headerName: 'Approver ID', flex: 1 },
    { field: 'level', headerName: 'Level', width: 80 },
    {
      field: 'action',
      headerName: 'Decision',
      width: 120,
      renderCell: (params) => (
        <Chip
          label={params.value as string}
          size="small"
          color={params.value === 'APPROVED' ? 'success' : 'error'}
        />
      ),
    },
    {
      field: 'actedAt',
      headerName: 'Decided At',
      width: 160,
      valueFormatter: (value) => value ? new Date(value as string).toLocaleString() : '—',
    },
    { field: 'comments', headerName: 'Comments', flex: 1 },
  ];

  return (
    <Box sx={{ p: 3 }} data-testid="approval-dashboard">
      <Typography variant="h4" sx={{ mb: 3 }}>
        Approval Workflow
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Stats cards */}
      <Grid container spacing={2} sx={{ mb: 3 }} data-testid="approval-stats">
        <Grid item xs={12} sm={4}>
          <Card>
            <CardContent>
              <Typography variant="h3" color="warning.main">
                {stats?.pending ?? '—'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Pending
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={4}>
          <Card>
            <CardContent>
              <Typography variant="h3" color="success.main">
                {stats?.approved ?? '—'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Approved Today
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={4}>
          <Card>
            <CardContent>
              <Typography variant="h3" color="error.main">
                {stats?.rejected ?? '—'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Rejected Today
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Tabs */}
      <Tabs value={activeTab} onChange={handleTabChange} sx={{ mb: 2 }}>
        <Tab label="Pending Approvals" />
        <Tab label="History" data-testid="approval-history-tab" />
      </Tabs>

      {activeTab === 0 && (
        <Box>
          {loadingPending ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
              <CircularProgress />
            </Box>
          ) : (
            <DataGrid
              rows={pending}
              columns={pendingColumns}
              autoHeight
              pageSizeOptions={[10, 25, 50]}
              initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
              disableRowSelectionOnClick
            />
          )}
        </Box>
      )}

      {activeTab === 1 && (
        <Box>
          {loadingHistory ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
              <CircularProgress />
            </Box>
          ) : (
            <DataGrid
              rows={history}
              columns={historyColumns}
              autoHeight
              pageSizeOptions={[10, 25, 50]}
              initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
              disableRowSelectionOnClick
            />
          )}
        </Box>
      )}

      {/* Escalate dialog */}
      <Dialog
        open={escalateDialogOpen}
        onClose={() => setEscalateDialogOpen(false)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>Escalate Approval</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            fullWidth
            multiline
            rows={3}
            label="Reason for escalation"
            value={escalateReason}
            onChange={(e) => setEscalateReason(e.target.value)}
            sx={{ mt: 1 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEscalateDialogOpen(false)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={handleEscalateConfirm}
            disabled={!escalateReason.trim()}
          >
            Escalate
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default ApprovalDashboardPage;
