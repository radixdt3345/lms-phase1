import React from 'react';
import { Navigate } from 'react-router-dom';
import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import { InteractionStatus } from '@azure/msal-browser';
import { Box, CircularProgress } from '@mui/material';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRoles?: string[];
}

/**
 * Wraps a route so it is accessible only to authenticated users.
 * While MSAL is handling a redirect or interaction, renders a spinner.
 * If the user is not authenticated, redirects to /login.
 * If requiredRoles are specified, redirects to /login when the user lacks them.
 */
const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, requiredRoles }) => {
  const isAuthenticated = useIsAuthenticated();
  const { inProgress, accounts } = useMsal();

  // Wait for MSAL to finish any in-flight interaction (e.g. redirect callback)
  if (inProgress !== InteractionStatus.None) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // Optional role check using the idTokenClaims on the active account
  if (requiredRoles && requiredRoles.length > 0) {
    const account = accounts[0];
    const tokenRoles: string[] =
      (account?.idTokenClaims as { roles?: string[] })?.roles ?? [];
    const hasRole = requiredRoles.some((r) => tokenRoles.includes(r));
    if (!hasRole) {
      return <Navigate to="/login" replace />;
    }
  }

  return <>{children}</>;
};

export default ProtectedRoute;
