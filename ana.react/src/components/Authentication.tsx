import React, { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const Authentication = () => {
  const { action } = useParams();
  const { userManager } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    const handleAuthAction = async () => {
      if (!userManager) return;

      try {
        switch (action) {
          case 'login':
            await userManager.signinRedirect();
            break;
            
          case 'login-callback':
            const user = await userManager.signinRedirectCallback();
            const state = user?.state as { returnUrl?: string } | undefined;
            const returnUrl = state?.returnUrl || '/';
            navigate(returnUrl, { replace: true });
            break;
            
          case 'logout':
            await userManager.signoutRedirect();
            break;
            
          case 'logout-callback':
            await userManager.signoutRedirectCallback();
            navigate('/', { replace: true });
            break;
            
          case 'silent-callback':
            await userManager.signinSilentCallback();
            break;
            
          default:
            navigate('/', { replace: true });
        }
      } catch (error) {
        console.error(`Authentication ${action} failed:`, error);
        navigate('/error', { replace: true });
      }
    };

    handleAuthAction();
  }, [action, userManager, navigate]);

  const getDisplayMessage = () => {
    switch (action) {
      case 'login':
        return 'Redirecting to login...';
      case 'login-callback':
        return 'Processing login...';
      case 'logout':
        return 'Signing out...';
      case 'logout-callback':
        return 'Processing logout...';
      default:
        return 'Processing authentication...';
    }
  };

  return (
    <div className="authentication-page">
      <div className="text-center">
        <div className="spinner-border" role="status">
          <span className="sr-only">Loading...</span>
        </div>
        <p>{getDisplayMessage()}</p>
      </div>
    </div>
  );
};

export default Authentication;