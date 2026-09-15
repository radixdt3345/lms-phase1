import React, { useState } from 'react';
import { useMsal } from '@azure/msal-react';
import { loginRequest } from '../../auth/msalConfig';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  CircularProgress,
} from '@mui/material';

const LoginPage: React.FC = () => {
  const { instance } = useMsal();
  const [loading, setLoading] = useState(false);

  const handleSignIn = async () => {
    setLoading(true);
    try {
      await instance.loginRedirect(loginRequest);
    } catch (err) {
      console.error('Login error:', err);
      setLoading(false);
    }
  };

  return (
    <Box
      data-testid="login-page"
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'background.default',
      }}
    >
      <Card sx={{ maxWidth: 400, width: '100%', p: 2 }}>
        <CardContent sx={{ textAlign: 'center' }}>
          {/* LMS Logo Placeholder */}
          <Box
            sx={{
              width: 80,
              height: 80,
              bgcolor: 'primary.main',
              borderRadius: '50%',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              mx: 'auto',
              mb: 2,
            }}
          >
            <Typography variant="h5" sx={{ color: 'white', fontWeight: 'bold' }}>
              LMS
            </Typography>
          </Box>

          <Typography variant="h5" component="h1" gutterBottom>
            Leave Management System
          </Typography>

          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
            Sign in with your organizational account
          </Typography>

          {loading ? (
            <CircularProgress data-testid="loading-spinner" />
          ) : (
            <Button
              data-testid="sign-in-button"
              variant="contained"
              color="primary"
              size="large"
              fullWidth
              onClick={handleSignIn}
            >
              Sign in with Microsoft
            </Button>
          )}
        </CardContent>
      </Card>
    </Box>
  );
};

export default LoginPage;
