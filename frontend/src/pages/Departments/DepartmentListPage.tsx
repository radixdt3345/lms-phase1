import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  Box, Button, CircularProgress, IconButton, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, Typography, Paper, Chip, Alert
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import { fetchDepartmentsThunk, deleteDepartmentThunk } from '../../store/departmentSlice';
import type { AppDispatch, RootState } from '../../store/store';
import type { DepartmentDto } from '../../types';
import DepartmentFormDialog from './DepartmentFormDialog';

const DepartmentListPage: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { departments, loading, error } = useSelector((state: RootState) => state.departments);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedDept, setSelectedDept] = useState<DepartmentDto | undefined>(undefined);

  useEffect(() => {
    dispatch(fetchDepartmentsThunk());
  }, [dispatch]);

  const handleAdd = () => {
    setSelectedDept(undefined);
    setDialogOpen(true);
  };

  const handleEdit = (dept: DepartmentDto) => {
    setSelectedDept(dept);
    setDialogOpen(true);
  };

  const handleDelete = (dept: DepartmentDto) => {
    if (window.confirm(`Delete department "${dept.name}"?`)) {
      dispatch(deleteDepartmentThunk(dept.id));
    }
  };

  // Show spinner only when loading and no data is available yet
  const showSpinner = loading && departments.length === 0;

  return (
    <div data-testid="department-list-page">
      <Box sx={{ p: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h5">Departments</Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            data-testid="add-department-btn"
            onClick={handleAdd}
          >
            Add Department
          </Button>
        </Box>

        {error && (
          <Alert severity="error" data-testid="departments-error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}

        {showSpinner ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
            <CircularProgress data-testid="loading-spinner" />
          </Box>
        ) : (
          <TableContainer component={Paper} data-testid="departments-table">
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Name</TableCell>
                  <TableCell>Code</TableCell>
                  <TableCell>Overlap Limit</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {departments.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={5} align="center">No departments found</TableCell>
                  </TableRow>
                ) : (
                  departments.map((dept) => (
                    <TableRow key={dept.id} data-testid={`dept-row-${dept.id}`}>
                      <TableCell>{dept.name}</TableCell>
                      <TableCell>{dept.code}</TableCell>
                      <TableCell>{dept.overlapLimit}</TableCell>
                      <TableCell>
                        <Chip label={dept.isActive ? 'Active' : 'Inactive'} color={dept.isActive ? 'success' : 'default'} size="small" />
                      </TableCell>
                      <TableCell>
                        <IconButton
                          size="small"
                          data-testid={`edit-dept-${dept.id}`}
                          onClick={() => handleEdit(dept)}
                        >
                          <EditIcon />
                        </IconButton>
                        <IconButton
                          size="small"
                          color="error"
                          data-testid={`delete-dept-${dept.id}`}
                          onClick={() => handleDelete(dept)}
                        >
                          <DeleteIcon />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}

        <DepartmentFormDialog
          open={dialogOpen}
          onClose={() => setDialogOpen(false)}
          department={selectedDept}
        />
      </Box>
    </div>
  );
};

export default DepartmentListPage;
