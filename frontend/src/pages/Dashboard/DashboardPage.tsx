import React, { useEffect, useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  CircularProgress,
  Grid,
  Tab,
  Tabs,
  Typography,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
} from '@mui/material';
import PeopleIcon from '@mui/icons-material/People';
import EventBusyIcon from '@mui/icons-material/EventBusy';
import PendingActionsIcon from '@mui/icons-material/PendingActions';
import AccessTimeIcon from '@mui/icons-material/AccessTime';
import { useDispatch, useSelector } from 'react-redux';
import type { AppDispatch, RootState } from '../../store/store';
import {
  loadDashboardOverview,
  loadTeamLeave,
  loadApprovalSummary,
  loadCompOffSummary,
} from '../../store/dashboardSlice';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => (
  <div role="tabpanel" hidden={value !== index}>
    {value === index && <Box sx={{ pt: 3 }}>{children}</Box>}
  </div>
);

const DashboardPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const [activeTab, setActiveTab] = useState(0);

  const {
    overview,
    teamLeave,
    approvalSummary,
    compOffSummary,
    loadingOverview,
    loadingTeamLeave,
    loadingApprovals,
    loadingCompOff,
    error,
  } = useSelector((state: RootState) => state.dashboard);

  useEffect(() => {
    dispatch(loadDashboardOverview());
    dispatch(loadTeamLeave());
    dispatch(loadApprovalSummary());
    dispatch(loadCompOffSummary());
  }, [dispatch]);

  const isLoading = loadingOverview || loadingTeamLeave || loadingApprovals || loadingCompOff;

  if (isLoading && !overview && !teamLeave && !approvalSummary && !compOffSummary) {
    return (
      <Box data-testid="loading-spinner" sx={{ display: 'flex', justifyContent: 'center', mt: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box data-testid="dashboard-page" sx={{ p: 3 }}>
      <Typography variant="h5" fontWeight={600} mb={3}>
        Dashboard
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* Overview stat cards */}
      <Grid container spacing={2} data-testid="overview-stats" mb={4}>
        <Grid item xs={12} sm={6} md={3}>
          <Card data-testid="employee-count-card" variant="outlined">
            <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
              <PeopleIcon color="primary" sx={{ fontSize: 40 }} />
              <Box>
                <Typography variant="h4" fontWeight={700}>
                  {overview?.totalEmployees ?? '--'}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Total Employees
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card data-testid="on-leave-count-card" variant="outlined">
            <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
              <EventBusyIcon color="warning" sx={{ fontSize: 40 }} />
              <Box>
                <Typography variant="h4" fontWeight={700}>
                  {overview?.onLeaveToday ?? '--'}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  On Leave Today
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card data-testid="pending-approvals-card" variant="outlined">
            <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
              <PendingActionsIcon color="error" sx={{ fontSize: 40 }} />
              <Box>
                <Typography variant="h4" fontWeight={700}>
                  {overview?.pendingApprovals ?? '--'}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Pending Approvals
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card data-testid="comp-off-credits-card" variant="outlined">
            <CardContent sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
              <AccessTimeIcon color="success" sx={{ fontSize: 40 }} />
              <Box>
                <Typography variant="h4" fontWeight={700}>
                  {overview?.availableCompOffCredits ?? '--'}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Available CompOff Credits
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider' }}>
        <Tabs value={activeTab} onChange={(_e, v) => setActiveTab(v)} aria-label="dashboard tabs">
          <Tab label="Overview" data-testid="overview-tab" />
          <Tab label="Team Leave" data-testid="team-leave-tab" />
          <Tab label="Approvals" data-testid="approvals-tab" />
          <Tab label="Comp Off" data-testid="comp-off-tab" />
        </Tabs>
      </Box>

      {/* Overview Tab */}
      <TabPanel value={activeTab} index={0}>
        <Typography variant="body1" color="text.secondary">
          Summary metrics are displayed in the cards above. Select a tab for detailed data.
        </Typography>
      </TabPanel>

      {/* Team Leave Tab */}
      <TabPanel value={activeTab} index={1}>
        {loadingTeamLeave ? (
          <CircularProgress size={24} />
        ) : teamLeave ? (
          <TableContainer component={Paper} variant="outlined">
            <Table size="small" data-testid="team-leave-table">
              <TableHead>
                <TableRow>
                  <TableCell>Employee</TableCell>
                  <TableCell>Leave Type</TableCell>
                  <TableCell>Start Date</TableCell>
                  <TableCell>End Date</TableCell>
                  <TableCell>Status</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {[...teamLeave.employeesOnLeave, ...teamLeave.pendingRequests].length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} align="center">
                      <Typography data-testid="team-leave-empty" color="text.secondary">
                        No team leave data available.
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  [...teamLeave.employeesOnLeave, ...teamLeave.pendingRequests].map((entry, idx) => (
                    <TableRow key={`${entry.employeeId}-${idx}`} data-testid={`team-leave-row-${entry.employeeId}`}>
                      <TableCell>{entry.employeeName}</TableCell>
                      <TableCell>{entry.leaveType}</TableCell>
                      <TableCell>{entry.startDate}</TableCell>
                      <TableCell>{entry.endDate}</TableCell>
                      <TableCell>
                        <Chip label={entry.status} size="small" color={entry.status === 'Approved' ? 'success' : 'warning'} />
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        ) : (
          <Typography color="text.secondary">No team leave data.</Typography>
        )}
      </TabPanel>

      {/* Approvals Tab */}
      <TabPanel value={activeTab} index={2}>
        {loadingApprovals ? (
          <CircularProgress size={24} />
        ) : approvalSummary ? (
          <Box>
            <Grid container spacing={2} mb={2}>
              <Grid item>
                <Typography variant="body2">Pending: <strong>{approvalSummary.pendingCount}</strong></Typography>
              </Grid>
              <Grid item>
                <Typography variant="body2">Approved Today: <strong>{approvalSummary.approvedToday}</strong></Typography>
              </Grid>
              <Grid item>
                <Typography variant="body2">Rejected Today: <strong>{approvalSummary.rejectedToday}</strong></Typography>
              </Grid>
              <Grid item>
                <Typography variant="body2">Avg Turnaround: <strong>{approvalSummary.averageTurnaroundHours}h</strong></Typography>
              </Grid>
            </Grid>
            <TableContainer component={Paper} variant="outlined">
              <Table size="small" data-testid="approvals-table">
                <TableHead>
                  <TableRow>
                    <TableCell>Employee</TableCell>
                    <TableCell>Leave Type</TableCell>
                    <TableCell>Start Date</TableCell>
                    <TableCell>End Date</TableCell>
                    <TableCell>Submitted</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {(approvalSummary.pendingApprovals ?? []).length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} align="center">
                        <Typography color="text.secondary">No pending approvals.</Typography>
                      </TableCell>
                    </TableRow>
                  ) : (
                    (approvalSummary.pendingApprovals ?? []).map((entry) => (
                      <TableRow key={entry.id} data-testid={`approval-row-${entry.id}`}>
                        <TableCell>{entry.employeeName}</TableCell>
                        <TableCell>{entry.leaveType}</TableCell>
                        <TableCell>{entry.startDate}</TableCell>
                        <TableCell>{entry.endDate}</TableCell>
                        <TableCell>{new Date(entry.submittedAt).toLocaleDateString()}</TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </Box>
        ) : (
          <Typography color="text.secondary">No approval data.</Typography>
        )}
      </TabPanel>

      {/* Comp Off Tab */}
      <TabPanel value={activeTab} index={3}>
        {loadingCompOff ? (
          <CircularProgress size={24} />
        ) : compOffSummary ? (
          <Box>
            <Grid container spacing={2} mb={2}>
              <Grid item>
                <Typography variant="body2">Total Credits Available: <strong>{compOffSummary.totalCreditsAvailable}</strong></Typography>
              </Grid>
              <Grid item>
                <Typography variant="body2">Expiring This Month: <strong>{compOffSummary.expiringThisMonth}</strong></Typography>
              </Grid>
              <Grid item>
                <Typography variant="body2">Credits Used This Month: <strong>{compOffSummary.creditsUsedThisMonth}</strong></Typography>
              </Grid>
            </Grid>
            <TableContainer component={Paper} variant="outlined">
              <Table size="small" data-testid="comp-off-table">
                <TableHead>
                  <TableRow>
                    <TableCell>Employee</TableCell>
                    <TableCell>Credits Available</TableCell>
                    <TableCell>Expiring Credits</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {(compOffSummary.creditsByEmployee ?? []).length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={3} align="center">
                        <Typography color="text.secondary">No comp-off data.</Typography>
                      </TableCell>
                    </TableRow>
                  ) : (
                    (compOffSummary.creditsByEmployee ?? []).map((entry) => (
                      <TableRow key={entry.employeeId} data-testid={`comp-off-row-${entry.employeeId}`}>
                        <TableCell>{entry.employeeName}</TableCell>
                        <TableCell>{entry.creditsAvailable}</TableCell>
                        <TableCell>{entry.expiringCredits}</TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </TableContainer>
          </Box>
        ) : (
          <Typography color="text.secondary">No comp-off data.</Typography>
        )}
      </TabPanel>
    </Box>
  );
};

export default DashboardPage;
