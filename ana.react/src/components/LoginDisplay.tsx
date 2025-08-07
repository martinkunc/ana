import React from 'react';
import { useAuth } from '../contexts/AuthContext';
import { useSharedState } from '../contexts/SharedStateContext';

interface LoginDisplayProps {
  className?: string;
}

const LoginDisplay: React.FC<LoginDisplayProps> = ({ className = '' }) => {
  const { isAuthenticated, isLoading, login, logout } = useAuth();
  const { displayName, isLoadingDisplayName } = useSharedState();


  const handleLogin = async () => {
    try {
      await login();
    } catch (error) {
      console.error('Login failed:', error);
    }
  };

  const handleLogout = async () => {
    try {
      await logout();
    } catch (error) {
      console.error('Logout failed:', error);
    }
  };

  if (isLoading || isLoadingDisplayName) {
    return (
      <div className={`login-display ${className}`}>
        <span>Loading...</span>
      </div>
    );
  }

  return (
    <div className={`login-display ${className}`}>
      {isAuthenticated ? (
        // Authorized view
        <div className="d-flex align-items-center">
          <span className="me-3">{displayName}</span>
          <button 
            className="nav-link btn btn-link" 
            onClick={handleLogout}
            type="button"
          >
            Log out
          </button>
        </div>
      ) : (
        // Not authorized view
        <button 
          className="nav-link btn btn-link" 
          onClick={handleLogin}
          type="button"
        >
          Log in
        </button>
      )}
    </div>
  );
};

export default LoginDisplay;