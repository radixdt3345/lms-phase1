import React, { useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import DownloadIcon from '@mui/icons-material/Download';
import { useDispatch, useSelector } from 'react-redux';
import type { AppDispatch, RootState } from '../../store/store';
import { loadReportJobs, generateReport } from '../../store/reportSlice';
import { downloadReport } from '../../api/reportApi';
import type { ReportType } from '../../api/reportApi';

const REPORT_TYPES: { value: ReportType; label: string }[] = [
  { value: 'LeaveSummary', label: 'Leave Summary' },
  { value: 'CompOffSummary', label: 'Comp-Off Summary' },
  { value: 'ApprovalHistory', label: 'Approval History' },
];

const statusColor = (status: string): 'default' | 'warning' | 'success' | 'error' => {
  switch (status) {
    case 'Completed': return 'success';
    case 'Pending': return 'warning';
    case 'Processing': return 'warning';
    case 'Failed': return 'error';
    default: return 'default';
  }
};

const ReportsPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();

  const [reportType, setReportType] = useState<ReportType>('LeaveSummary');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [departmentId, setDepartmentId] = useState('');
  const [downloadError, setDownloadError] = useState<string | null>(null);

  const { reportJobs, loading, generating, error } = useSelector(
    (state: RootState) => state.reports,
  );

  useEffect(() => {
    dispatch(loadReportJobs());
  }, [dispatch]);

  const handleGenerateReport = () => {
    dispatch(
      generateReport({
        reportType,
        startDate: startDate || undefined,
        endDate: endDate || undefined,
        departmentId: departmentId || undefined,
      }),
    );
  };

  const handleDownload = async (id: string) => {
    setDownloadError(null);
    try {
      const blob = await downloadReport(id);
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `report-${id}.csv`;
      link.click();
      URL.revokeObjectURL(url);
    } catch {
      setDownloadError(`Failed to download report ${id}.`);
    }
  };

  return (
    <Box data-testid="reports-page" sx={{ p: 3 }}>
      <Typography variant="h5" sx={{ fontWeight: 600, mb: 3 }}>
        Reports &amp; CSV Export
      </Typography>

      {(error || downloadError) && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error || downloadError}
        </Alert>
      )}

      {/* Report generation form */}
      <Paper variant="outlined" sx={{ p: 3, mb: 4 }}>
        <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
          Generate New Report
        </Typography>
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'flex-end' }}>
          <FormControl sx={{ minWidth: 200 }}>
            <InputLabel id="report-type-label">Report Type</InputLabel>
            <Select
              labelId="report-type-label"
              label="Report Type"
              value={reportType}
              onChange={(e) => setReportType(e.target.value as ReportType)}
              inputProps={{ 'data-testid': 'report-type-select' }}
            >
              {REPORT_TYPES.map((rt) => (
                <MenuItem key={rt.value} value={rt.value}>
                  {rt.label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <TextField
            label="Start Date"
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            slotProps={{ htmlInput: { 'data-testid': 'start-date-input' }, inputLabel: { shrink: true } }}
            sx={{ minWidth: 160 }}
          />

          <TextField
            label="End Date"
            type="date"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
            slotProps={{ htmlInput: { 'data-testid': 'end-date-input' }, inputLabel: { shrink: true } }}
            sx={{ minWidth: 160 }}
          />

          <TextField
            label="Department ID (optional)"
            value={departmentId}
            onChange={(e) => setDepartmentId(e.target.value)}
            slotProps={{ htmlInput: { 'data-testid': 'department-id-input' } }}
            sx={{ minWidth: 200 }}
          />

          <Button
            variant="contained"
            onClick={handleGenerateReport}
            disabled={generating}
            data-testid="generate-report-btn"
            startIcon={generating ? <CircularProgress size={16} /> : null}
          >
            {generating ? 'Generating...' : 'Generate Report'}
          </Button>
        </Box>
      </Paper>

      {/* Reports history table */}
      <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 2 }}>
        Report History
      </Typography>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
          <CircularProgress data-testid="loading-spinner" />
        </Box>
      ) : (
        <TableContainer component={Paper} variant="outlined">
          <Table data-testid="reports-table">
            <TableHead>
              <TableRow>
                <TableCell>Report Type</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Requested At</TableCell>
                <TableCell>Completed At</TableCell>
                <TableCell>Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {reportJobs.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5} align="center">
                    <Typography data-testid="empty-reports" color="text.secondary">
                      No reports generated yet.
                    </Typography>
                  </TableCell>
                </TableRow>
              ) : (
                reportJobs.map((job) => (
                  <TableRow key={job.id} data-testid={`report-row-${job.id}`}>
                    <TableCell>
                      {REPORT_TYPES.find((rt) => rt.value === job.reportType)?.label ?? job.reportType}
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={job.status}
                        size="small"
                        color={statusColor(job.status)}
                        data-testid={`report-status-${job.id}`}
                      />
                    </TableCell>
                    <TableCell>{new Date(job.requestedAt).toLocaleString()}</TableCell>
                    <TableCell>
                      {job.completedAt ? new Date(job.completedAt).toLocaleString() : '—'}
                    </TableCell>
                    <TableCell>
                      {job.status === 'Completed' && (
                        <Button
                          size="small"
                          variant="outlined"
                          startIcon={<DownloadIcon />}
                          onClick={() => handleDownload(job.id)}
                          data-testid={`download-report-btn-${job.id}`}
                        >
                          Download
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Box>
  );
};

export default ReportsPage;
