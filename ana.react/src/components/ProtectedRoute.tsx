import React from 'react';
import { useAuth } from '../contexts/AuthContext';
import { useLocation } from 'react-router-dom';

const ProtectedRoute: React.FC<React.PropsWithChildren<{}>> = ({ children }) => {
  const { isAuthenticated, isLoading, login } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="text-center">
        <div className="spinner-border" role="status">
          <span className="sr-only">Loading...</span>
        </div>
        <p>Authorizing...</p>
      </div>
    );
  }

  if (!isAuthenticated) {
    // Redirect to login with current location as return URL
    login(location.pathname + location.search);
    return null;
  }

  return children;
};

export default ProtectedRoute;