import React, { useState, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import {
  Dialog, DialogTitle, DialogContent, DialogActions,
  Button, TextField, Box, MenuItem,
} from '@mui/material';
import { createEmployeeThunk, updateEmployeeThunk } from '../../store/employeeSlice';
import type { AppDispatch, RootState } from '../../store/store';
import type { EmployeeProfileDto, EmploymentType, EmployeeStatus } from '../../types';

interface Props {
  open: boolean;
  onClose: (saved: boolean) => void;
  employee?: EmployeeProfileDto;
}

const EMPLOYMENT_TYPES: EmploymentType[] = ['FullTime', 'PartTime', 'Contract', 'Intern'];
const EMPLOYEE_STATUSES: EmployeeStatus[] = ['Active', 'Inactive', 'OnLeave', 'Terminated'];

const EmployeeFormDialog: React.FC<Props> = ({ open, onClose, employee }) => {
  const dispatch = useDispatch<AppDispatch>();
  const { departments } = useSelector((state: RootState) => state.departments);

  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [email, setEmail] = useState('');
  const [employeeCode, setEmployeeCode] = useState('');
  const [jobTitle, setJobTitle] = useState('');
  const [departmentId, setDepartmentId] = useState('');
  const [dateOfJoining, setDateOfJoining] = useState('');
  const [employmentType, setEmploymentType] = useState<EmploymentType>('FullTime');
  const [status, setStatus] = useState<EmployeeStatus>('Active');

  useEffect(() => {
    if (employee) {
      setFirstName(employee.firstName);
      setLastName(employee.lastName);
      setEmail(employee.email);
      setEmployeeCode(employee.employeeCode);
      setJobTitle(employee.jobTitle);
      setDepartmentId(employee.departmentId);
      setDateOfJoining(employee.dateOfJoining ? employee.dateOfJoining.slice(0, 10) : '');
      setEmploymentType(employee.employmentType);
      setStatus(employee.status);
    } else {
      setFirstName('');
      setLastName('');
      setEmail('');
      setEmployeeCode('');
      setJobTitle('');
      setDepartmentId('');
      setDateOfJoining('');
      setEmploymentType('FullTime');
      setStatus('Active');
    }
  }, [employee, open]);

  const handleSubmit = async () => {
    if (!firstName || !lastName || !email || !employeeCode || !jobTitle || !departmentId || !dateOfJoining) return;

    if (employee) {
      await dispatch(updateEmployeeThunk({
        id: employee.id,
        dto: { firstName, lastName, email, jobTitle, departmentId, dateOfJoining, employmentType, status },
      }));
    } else {
      await dispatch(createEmployeeThunk({
        firstName, lastName, email, employeeCode, jobTitle,
        departmentId, dateOfJoining, employmentType, status,
      }));
    }
    onClose(true);
  };

  const handleCancel = () => {
    onClose(false);
  };

  return (
    <Dialog open={open} onClose={handleCancel} data-testid="employee-form-dialog" maxWidth="sm" fullWidth>
      <DialogTitle>{employee ? 'Edit Employee' : 'Add Employee'}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <TextField
            label="First Name"
            value={firstName}
            onChange={(e) => setFirstName(e.target.value)}
            required
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'employee-firstname-input' } }}
          />
          <TextField
            label="Last Name"
            value={lastName}
            onChange={(e) => setLastName(e.target.value)}
            required
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'employee-lastname-input' } }}
          />
          <TextField
            label="Email"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'employee-email-input' } }}
          />
          <TextField
            label="Employee Code"
            value={employeeCode}
            onChange={(e) => setEmployeeCode(e.target.value)}
            required
            fullWidth
            disabled={!!employee}
            slotProps={{ htmlInput: { 'data-testid': 'employee-code-input' } }}
          />
          <TextField
            label="Job Title"
            value={jobTitle}
            onChange={(e) => setJobTitle(e.target.value)}
            required
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'employee-jobtitle-input' } }}
          />
          <TextField
            select
            label="Department"
            value={departmentId}
            onChange={(e) => setDepartmentId(e.target.value)}
            required
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'employee-department-input' } }}
          >
            <MenuItem value="">Select Department</MenuItem>
            {departments.map((dept) => (
              <MenuItem key={dept.id} value={dept.id}>
                {dept.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            label="Date of Joining"
            type="date"
            value={dateOfJoining}
            onChange={(e) => setDateOfJoining(e.target.value)}
            required
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'employee-doj-input' }, inputLabel: { shrink: true } }}
          />
          <TextField
            select
            label="Employment Type"
            value={employmentType}
            onChange={(e) => setEmploymentType(e.target.value as EmploymentType)}
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'employee-type-input' } }}
          >
            {EMPLOYMENT_TYPES.map((t) => (
              <MenuItem key={t} value={t}>{t}</MenuItem>
            ))}
          </TextField>
          <TextField
            select
            label="Status"
            value={status}
            onChange={(e) => setStatus(e.target.value as EmployeeStatus)}
            fullWidth
            slotProps={{ htmlInput: { 'data-testid': 'employee-status-input' } }}
          >
            {EMPLOYEE_STATUSES.map((s) => (
              <MenuItem key={s} value={s}>{s}</MenuItem>
            ))}
          </TextField>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleCancel} data-testid="employee-cancel-btn">Cancel</Button>
        <Button onClick={handleSubmit} variant="contained" data-testid="employee-submit-btn">
          {employee ? 'Update' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default EmployeeFormDialog;
