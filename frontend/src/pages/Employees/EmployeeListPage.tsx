import React, { useEffect, useState, useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box, Button, CircularProgress, IconButton, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, Typography,
  Paper, Chip, Alert, TextField, TablePagination, MenuItem,
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import { fetchEmployeesThunk, deleteEmployeeThunk } from '../../store/employeeSlice';
import { fetchDepartmentsThunk } from '../../store/departmentSlice';
import type { AppDispatch, RootState } from '../../store/store';
import type { EmployeeProfileDto } from '../../types';
import EmployeeFormDialog from './EmployeeFormDialog';

const EmployeeListPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { employees, loading, error, totalCount, pageNumber, pageSize } = useSelector(
    (state: RootState) => state.employees
  );
  const { departments } = useSelector((state: RootState) => state.departments);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedEmployee, setSelectedEmployee] = useState<EmployeeProfileDto | undefined>(undefined);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(20);
  const [departmentFilter, setDepartmentFilter] = useState('');

  const loadEmployees = useCallback(() => {
    dispatch(fetchEmployeesThunk({
      pageNumber: page + 1,
      pageSize: rowsPerPage,
      search: search || undefined,
      departmentId: departmentFilter || undefined,
    }));
  }, [dispatch, page, rowsPerPage, search, departmentFilter]);

  useEffect(() => {
    dispatch(fetchDepartmentsThunk());
  }, [dispatch]);

  useEffect(() => {
    loadEmployees();
  }, [loadEmployees]);

  const handleAdd = () => {
    setSelectedEmployee(undefined);
    setDialogOpen(true);
  };

  const handleEdit = (emp: EmployeeProfileDto) => {
    setSelectedEmployee(emp);
    setDialogOpen(true);
  };

  const handleDelete = (emp: EmployeeProfileDto) => {
    if (window.confirm(`Delete employee "${emp.firstName} ${emp.lastName}"?`)) {
      dispatch(deleteEmployeeThunk(emp.id)).then(() => {
        loadEmployees();
      });
    }
  };

  const handleDialogClose = (saved: boolean) => {
    setDialogOpen(false);
    if (saved) {
      loadEmployees();
    }
  };

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearch(e.target.value);
    setPage(0);
  };

  const handleDepartmentFilterChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setDepartmentFilter(e.target.value);
    setPage(0);
  };

  const handleChangePage = (_: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (e: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(e.target.value, 10));
    setPage(0);
  };

  const showSpinner = loading && employees.length === 0;

  const statusColor = (status: string) => {
    switch (status) {
      case 'Active': return 'success';
      case 'Inactive': return 'default';
      case 'OnLeave': return 'warning';
      case 'Terminated': return 'error';
      default: return 'default';
    }
  };

  return (
    <div data-testid="employee-list-page">
      <Box sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h5">Employees</Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            data-testid="add-employee-btn"
            onClick={handleAdd}
          >
            Add Employee
          </Button>
        </Box>

        <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
          <TextField
            label="Search employees"
            value={search}
            onChange={handleSearchChange}
            size="small"
            slotProps={{ htmlInput: { 'data-testid': 'employee-search-input' } }}
          />
          <TextField
            select
            label="Filter by Department"
            value={departmentFilter}
            onChange={handleDepartmentFilterChange}
            size="small"
            sx={{ minWidth: 200 }}
            slotProps={{ htmlInput: { 'data-testid': 'employee-dept-filter' } }}
          >
            <MenuItem value="">All Departments</MenuItem>
            {departments.map((dept) => (
              <MenuItem key={dept.id} value={dept.id}>
                {dept.name}
              </MenuItem>
            ))}
          </TextField>
        </Box>

        {error && (
          <Alert severity="error" data-testid="employees-error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {showSpinner ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
            <CircularProgress data-testid="loading-spinner" />
          </Box>
        ) : employees.length === 0 ? (
          <Box sx={{ textAlign: 'center', mt: 4 }} data-testid="empty-employees">
            <Typography color="text.secondary">No employees found</Typography>
          </Box>
        ) : (
          <TableContainer component={Paper} data-testid="employee-table">
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Code</TableCell>
                  <TableCell>Name</TableCell>
                  <TableCell>Email</TableCell>
                  <TableCell>Job Title</TableCell>
                  <TableCell>Department</TableCell>
                  <TableCell>Type</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {employees.map((emp) => (
                  <TableRow key={emp.id} data-testid={`employee-row-${emp.id}`}>
                    <TableCell>{emp.employeeCode}</TableCell>
                    <TableCell>{emp.firstName} {emp.lastName}</TableCell>
                    <TableCell>{emp.email}</TableCell>
                    <TableCell>{emp.jobTitle}</TableCell>
                    <TableCell>{emp.departmentName}</TableCell>
                    <TableCell>{emp.employmentType}</TableCell>
                    <TableCell>
                      <Chip
                        label={emp.status}
                        color={statusColor(emp.status) as 'success' | 'default' | 'warning' | 'error'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell>
                      <IconButton
                        size="small"
                        data-testid={`edit-employee-${emp.id}`}
                        onClick={() => handleEdit(emp)}
                      >
                        <EditIcon />
                      </IconButton>
                      <IconButton
                        size="small"
                        color="error"
                        data-testid={`delete-employee-${emp.id}`}
                        onClick={() => handleDelete(emp)}
                      >
                        <DeleteIcon />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            <TablePagination
              component="div"
              count={totalCount}
              page={page}
              onPageChange={handleChangePage}
              rowsPerPage={rowsPerPage}
              onRowsPerPageChange={handleChangeRowsPerPage}
              rowsPerPageOptions={[10, 20, 50]}
            />
          </TableContainer>
        )}

        <EmployeeFormDialog
          open={dialogOpen}
          onClose={handleDialogClose}
          employee={selectedEmployee}
        />
      </Box>
    </div>
  );
};

export default EmployeeListPage;
