import React, { useEffect } from 'react';
import {
  Box,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Button,
  Chip,
  CircularProgress,
  Alert,
  Tooltip,
} from '@mui/material';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import RefreshIcon from '@mui/icons-material/Refresh';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState, AppDispatch } from '../../store/store';
import { loadJobStatuses, triggerJobAsync } from '../../store/jobSlice';

const KNOWN_JOBS = [
  'LeaveBalanceSyncJob',
  'CompOffExpiryJob',
  'EmailDispatchJob',
  'LeaveEscalationJob',
];

const statusColor = (status: string | null): 'success' | 'error' | 'warning' | 'default' => {
  if (!status) return 'default';
  if (status.toLowerCase() === 'succeeded') return 'success';
  if (status.toLowerCase() === 'failed') return 'error';
  return 'warning';
};

const JobAdminPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { jobs, loading, error, triggeringJob } = useSelector((s: RootState) => s.jobs);

  useEffect(() => {
    dispatch(loadJobStatuses());
  }, [dispatch]);

  const handleTrigger = (jobName: string) => {
    dispatch(triggerJobAsync({ jobName, reason: 'Manual trigger from admin UI' })).then(() => {
      dispatch(loadJobStatuses());
    });
  };

  const handleRefresh = () => {
    dispatch(loadJobStatuses());
  };

  return (
    <Box sx={{ p: 3 }} data-testid="job-admin-page">
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h4">Background Jobs</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
            Hangfire scheduled jobs — HRAdmin/SuperAdmin only.
            The full Hangfire dashboard is available at{' '}
            <strong>/hangfire</strong> (backend route, not a React route).
          </Typography>
        </Box>
        <Tooltip title="Refresh status">
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={handleRefresh}
            disabled={loading}
          >
            Refresh
          </Button>
        </Tooltip>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {loading && jobs.length === 0 ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 6 }}>
          <CircularProgress />
        </Box>
      ) : (
        <TableContainer component={Paper} data-testid="job-table">
          <Table>
            <TableHead>
              <TableRow>
                <TableCell><strong>Job Name</strong></TableCell>
                <TableCell><strong>Schedule</strong></TableCell>
                <TableCell><strong>Last Run</strong></TableCell>
                <TableCell><strong>Status</strong></TableCell>
                <TableCell><strong>Duration</strong></TableCell>
                <TableCell><strong>Next Run</strong></TableCell>
                <TableCell><strong>Actions</strong></TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {jobs.length === 0
                ? KNOWN_JOBS.map((name) => (
                    <TableRow key={name}>
                      <TableCell data-testid={`job-status-${name}`}>{name}</TableCell>
                      <TableCell>—</TableCell>
                      <TableCell>—</TableCell>
                      <TableCell>
                        <Chip label="Unknown" size="small" />
                      </TableCell>
                      <TableCell>—</TableCell>
                      <TableCell>—</TableCell>
                      <TableCell>
                        <Button
                          size="small"
                          variant="contained"
                          color="primary"
                          startIcon={
                            triggeringJob === name ? (
                              <CircularProgress size={14} color="inherit" />
                            ) : (
                              <PlayArrowIcon />
                            )
                          }
                          disabled={!!triggeringJob}
                          onClick={() => handleTrigger(name)}
                          data-testid={`trigger-job-btn-${name}`}
                        >
                          Trigger
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))
                : jobs.map((job) => (
                    <TableRow key={job.jobName}>
                      <TableCell data-testid={`job-status-${job.jobName}`}>
                        <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
                          {job.jobName}
                        </Typography>
                        {!job.isEnabled && (
                          <Chip label="Disabled" size="small" color="warning" sx={{ ml: 1 }} />
                        )}
                      </TableCell>
                      <TableCell>
                        <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>
                          {job.cronExpression || '—'}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        {job.lastRunAt
                          ? new Date(job.lastRunAt).toLocaleString()
                          : '—'}
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={job.lastRunStatus ?? 'Never run'}
                          size="small"
                          color={statusColor(job.lastRunStatus)}
                        />
                      </TableCell>
                      <TableCell>
                        {job.lastRunDurationMs != null
                          ? `${(job.lastRunDurationMs / 1000).toFixed(1)}s`
                          : '—'}
                      </TableCell>
                      <TableCell>
                        {job.nextRunAt
                          ? new Date(job.nextRunAt).toLocaleString()
                          : '—'}
                      </TableCell>
                      <TableCell>
                        <Tooltip
                          title={
                            !job.isEnabled
                              ? 'Job is disabled'
                              : 'Trigger now (SuperAdmin only)'
                          }
                        >
                          <span>
                            <Button
                              size="small"
                              variant="contained"
                              color="primary"
                              startIcon={
                                triggeringJob === job.jobName ? (
                                  <CircularProgress size={14} color="inherit" />
                                ) : (
                                  <PlayArrowIcon />
                                )
                              }
                              disabled={!!triggeringJob || !job.isEnabled}
                              onClick={() => handleTrigger(job.jobName)}
                              data-testid={`trigger-job-btn-${job.jobName}`}
                            >
                              Trigger
                            </Button>
                          </span>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                  ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Box>
  );
};

export default JobAdminPage;
