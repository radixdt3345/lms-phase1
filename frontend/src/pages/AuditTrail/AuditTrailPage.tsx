import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Collapse,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Pagination,
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
import ClearIcon from '@mui/icons-material/Clear';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import { fetchAuditLogsThunk, setFilters, setPage, clearFilters } from '../../store/auditLogSlice';
import type { AppDispatch, RootState } from '../../store/store';
import type { AuditLogDto, AuditLogFilters } from '../../types';

const ACTION_TYPES = [
  'CREATE', 'UPDATE', 'DELETE', 'APPROVE', 'REJECT',
  'CANCEL', 'REVOKE', 'LOGIN', 'LOGOUT', 'LOCK', 'UNLOCK',
];

const RECORD_TYPES = [
  'Employee', 'Leave', 'CompOff', 'LeaveType',
  'Holiday', 'Department', 'Role', 'Account',
];

interface AuditRowProps {
  log: AuditLogDto;
}

const AuditRow: React.FC<AuditRowProps> = ({ log }) => {
  const [expanded, setExpanded] = useState(false);
  const hasJson = log.oldValue !== null || log.newValue !== null;

  let parsedOld: unknown = null;
  let parsedNew: unknown = null;
  if (hasJson) {
    try { parsedOld = log.oldValue ? JSON.parse(log.oldValue) : null; } catch { parsedOld = log.oldValue; }
    try { parsedNew = log.newValue ? JSON.parse(log.newValue) : null; } catch { parsedNew = log.newValue; }
  }

  return (
    <>
      <TableRow>
        <TableCell sx={{ whiteSpace: 'nowrap', fontSize: '0.8rem' }}>
          {new Date(log.timestamp).toLocaleString()}
        </TableCell>
        <TableCell sx={{ fontSize: '0.8rem' }}>{log.actorEmail}</TableCell>
        <TableCell>
          <Chip label={log.actionType} size="small" variant="outlined" />
        </TableCell>
        <TableCell sx={{ fontSize: '0.8rem' }}>{log.recordType}</TableCell>
        <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>{log.recordId}</TableCell>
        <TableCell sx={{ fontSize: '0.8rem' }}>{log.ipAddress}</TableCell>
        <TableCell>
          {hasJson && (
            <IconButton
              size="small"
              data-testid="audit-json-diff-expand"
              onClick={() => setExpanded((p) => !p)}
              aria-label={expanded ? 'collapse json diff' : 'expand json diff'}
            >
              {expanded ? <ExpandLessIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
            </IconButton>
          )}
        </TableCell>
      </TableRow>
      {hasJson && (
        <TableRow>
          <TableCell colSpan={7} sx={{ py: 0 }}>
            <Collapse in={expanded} timeout="auto" unmountOnExit>
              <Box sx={{ display: 'flex', gap: 2, p: 1, bgcolor: 'grey.50', borderRadius: 1, my: 1 }}>
                <Box sx={{ flex: 1 }}>
                  <Typography variant="caption" sx={{ fontWeight: 'bold' }} color="error.main">Before</Typography>
                  <pre style={{ fontSize: '0.72rem', margin: 0, whiteSpace: 'pre-wrap', wordBreak: 'break-all' }}>
                    {JSON.stringify(parsedOld, null, 2)}
                  </pre>
                </Box>
                <Box sx={{ flex: 1 }}>
                  <Typography variant="caption" sx={{ fontWeight: 'bold' }} color="success.main">After</Typography>
                  <pre style={{ fontSize: '0.72rem', margin: 0, whiteSpace: 'pre-wrap', wordBreak: 'break-all' }}>
                    {JSON.stringify(parsedNew, null, 2)}
                  </pre>
                </Box>
              </Box>
            </Collapse>
          </TableCell>
        </TableRow>
      )}
    </>
  );
};

const AuditTrailPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { items, totalCount, loading, error, filters } = useSelector(
    (state: RootState) => state.auditLog,
  );

  const [userInput, setUserInput] = useState(filters.userId ?? '');
  const [actionTypeInput, setActionTypeInput] = useState(filters.actionType ?? '');
  const [recordTypeInput, setRecordTypeInput] = useState(filters.recordType ?? '');
  const [dateFromInput, setDateFromInput] = useState(filters.dateFrom ?? '');
  const [dateToInput, setDateToInput] = useState(filters.dateTo ?? '');

  const pageCount = Math.ceil(totalCount / filters.pageSize) || 1;

  useEffect(() => {
    dispatch(fetchAuditLogsThunk(filters));
  }, [dispatch, filters]);

  const handleApplyFilters = () => {
    dispatch(
      setFilters({
        userId: userInput || undefined,
        actionType: actionTypeInput || undefined,
        recordType: recordTypeInput || undefined,
        dateFrom: dateFromInput || undefined,
        dateTo: dateToInput || undefined,
      }),
    );
  };

  const handleClearFilters = () => {
    setUserInput('');
    setActionTypeInput('');
    setRecordTypeInput('');
    setDateFromInput('');
    setDateToInput('');
    dispatch(clearFilters());
  };

  const handlePageChange = (_event: React.ChangeEvent<unknown>, value: number) => {
    dispatch(setPage(value));
  };

  const removeFilter = (key: keyof AuditLogFilters) => {
    const update: Partial<AuditLogFilters> = { [key]: undefined };
    dispatch(setFilters(update));
    if (key === 'userId') setUserInput('');
    if (key === 'actionType') setActionTypeInput('');
    if (key === 'recordType') setRecordTypeInput('');
    if (key === 'dateFrom') setDateFromInput('');
    if (key === 'dateTo') setDateToInput('');
  };

  const activeFilters: { label: string; key: keyof AuditLogFilters }[] = [];
  if (filters.userId) activeFilters.push({ label: `User: ${filters.userId}`, key: 'userId' });
  if (filters.actionType) activeFilters.push({ label: `Action: ${filters.actionType}`, key: 'actionType' });
  if (filters.recordType) activeFilters.push({ label: `Record: ${filters.recordType}`, key: 'recordType' });
  if (filters.dateFrom) activeFilters.push({ label: `From: ${filters.dateFrom}`, key: 'dateFrom' });
  if (filters.dateTo) activeFilters.push({ label: `To: ${filters.dateTo}`, key: 'dateTo' });

  const showSpinner = loading && items.length === 0;

  return (
    <Box sx={{ p: 3 }} data-testid="audit-trail-page">
      <Typography variant="h5" sx={{ mb: 2 }}>
        Audit Trail
      </Typography>

      {/* Filter Panel */}
      <Paper sx={{ p: 2, mb: 2 }} elevation={2}>
        <Typography variant="subtitle2" sx={{ mb: 1 }}>
          Search &amp; Filter
        </Typography>
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, alignItems: 'flex-end', mb: 2 }}>
          {/* User ID filter */}
          <Box data-testid="audit-filter-user">
            <TextField
              label="User ID"
              size="small"
              value={userInput}
              onChange={(e) => setUserInput(e.target.value)}
              sx={{ minWidth: 160 }}
            />
          </Box>

          {/* Action type filter */}
          <Box data-testid="audit-filter-action-type">
            <FormControl size="small" sx={{ minWidth: 180 }}>
              <InputLabel>Action Type</InputLabel>
              <Select
                label="Action Type"
                value={actionTypeInput}
                onChange={(e) => setActionTypeInput(e.target.value)}
              >
                <MenuItem value="">All</MenuItem>
                {ACTION_TYPES.map((t) => (
                  <MenuItem key={t} value={t}>{t}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Box>

          {/* Record type filter */}
          <Box data-testid="audit-filter-record-type">
            <FormControl size="small" sx={{ minWidth: 180 }}>
              <InputLabel>Record Type</InputLabel>
              <Select
                label="Record Type"
                value={recordTypeInput}
                onChange={(e) => setRecordTypeInput(e.target.value)}
              >
                <MenuItem value="">All</MenuItem>
                {RECORD_TYPES.map((r) => (
                  <MenuItem key={r} value={r}>{r}</MenuItem>
                ))}
              </Select>
            </FormControl>
          </Box>

          {/* Date from filter */}
          <Box data-testid="audit-filter-date-from">
            <TextField
              label="Date From"
              type="date"
              size="small"
              value={dateFromInput}
              onChange={(e) => setDateFromInput(e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              sx={{ minWidth: 160 }}
            />
          </Box>

          {/* Date to filter */}
          <Box data-testid="audit-filter-date-to">
            <TextField
              label="Date To"
              type="date"
              size="small"
              value={dateToInput}
              onChange={(e) => setDateToInput(e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              sx={{ minWidth: 160 }}
            />
          </Box>

          <Button variant="contained" size="small" onClick={handleApplyFilters}>
            Apply
          </Button>

          <Button
            variant="outlined"
            size="small"
            startIcon={<ClearIcon />}
            data-testid="audit-clear-filters-button"
            onClick={handleClearFilters}
          >
            Clear
          </Button>
        </Box>

        {activeFilters.length > 0 && (
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {activeFilters.map((f) => (
              <Chip
                key={f.key}
                label={f.label}
                size="small"
                data-testid="audit-filter-chip"
                onDelete={() => removeFilter(f.key)}
              />
            ))}
          </Box>
        )}
      </Paper>

      {/* Error alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Loading spinner */}
      {showSpinner ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
          <CircularProgress data-testid="audit-loading-spinner" />
        </Box>
      ) : (
        <TableContainer component={Paper} data-testid="audit-log-table">
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Timestamp</TableCell>
                <TableCell>Actor</TableCell>
                <TableCell>Action</TableCell>
                <TableCell>Record Type</TableCell>
                <TableCell>Record ID</TableCell>
                <TableCell>IP Address</TableCell>
                <TableCell>Diff</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} align="center" data-testid="audit-empty-state">
                    No audit log entries found
                  </TableCell>
                </TableRow>
              ) : (
                items.map((log) => <AuditRow key={log.id} log={log} />)
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      {/* Pagination */}
      {!showSpinner && totalCount > filters.pageSize && (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
          <Pagination
            count={pageCount}
            page={filters.page}
            onChange={handlePageChange}
            data-testid="audit-pagination"
            color="primary"
          />
        </Box>
      )}

      <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
        Total: {totalCount} record{totalCount !== 1 ? 's' : ''}.
        Audit log is immutable — no record can be edited or deleted by any role.
      </Typography>
    </Box>
  );
};

export default AuditTrailPage;
