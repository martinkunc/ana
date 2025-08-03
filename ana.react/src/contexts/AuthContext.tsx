import React, { createContext, useContext, useState, useEffect } from 'react';
import { UserManager, WebStorageStateStore, User } from 'oidc-client-ts';
import { PUBLIC_URLS, APP_CONFIG, AUTH_URLS } from '../config/config';

interface AuthContextType {
  user: any;
  isAuthenticated: boolean;
  isLoading: boolean;
  userManager: UserManager | null;
  login: (returnUrl?: string) => Promise<void>;
  logout: () => Promise<void>;
  getAccessToken: () => string | null;
  getIdToken: () => string | null;
  getUserClaims: () => any;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [userManager, setUserManager] = useState<UserManager | null>(null);
  
  useEffect(() => {
    
    const oidcConfig = {
      authority: PUBLIC_URLS.OIDC_AUTHORITY,
      client_id: APP_CONFIG.OIDC_CLIENT_ID,
      redirect_uri: `${window.location.origin}/authentication/login-callback`,
      post_logout_redirect_uri: `${window.location.origin}/authentication/logout-callback`,
      response_type: 'code',
      scope: APP_CONFIG.OIDC_SCOPES,
      automaticSilentRenew: true,
      silent_redirect_uri: `${window.location.origin}/authentication/silent-callback`,
      userStore: new WebStorageStateStore({ store: window.localStorage }),
      includeIdTokenInSilentRenew: true,
      accessTokenExpiringNotificationTime: 60,
    };

    const manager = new UserManager(oidcConfig);
    setUserManager(manager);

    // Event handlers
    const handleUserLoaded = async (user: User) => {
      console.log('User loaded:', user);
      setUser(user);
      setIsAuthenticated(!!user && !user.expired);
    };

    const handleUserUnloaded = () => {
      console.log('User unloaded');
      setUser(null);
      setIsAuthenticated(false);
    };

    const handleAccessTokenExpired = () => {
      console.log('Access token expired');
      setUser(null);
      setIsAuthenticated(false);
    };

    const handleAccessTokenExpiring = () => {
      console.log('Access token expiring, attempting silent renew');
    };

    const handleSilentRenewError = (error: Error) => {
      console.warn('Silent renew failed:', error);
    };

    manager.events.addUserLoaded(handleUserLoaded);
    manager.events.addUserUnloaded(handleUserUnloaded);
    manager.events.addAccessTokenExpired(handleAccessTokenExpired);
    manager.events.addAccessTokenExpiring(handleAccessTokenExpiring);
    manager.events.addSilentRenewError(handleSilentRenewError);

    const initializeAuth = async () => {
      try {
        const currentUser = await manager.getUser();
        if (currentUser && !currentUser.expired) {
          setUser(currentUser);
          setIsAuthenticated(true);
        } else {
          setUser(null);
          setIsAuthenticated(false);
        }
      } catch (error) {
        console.error('Error initializing auth:', error);
        setUser(null);
        setIsAuthenticated(false);
      } finally {
        setIsLoading(false);
      }
    };

    initializeAuth();

    return () => {
      manager.events.removeUserLoaded(handleUserLoaded);
      manager.events.removeUserUnloaded(handleUserUnloaded);
      manager.events.removeAccessTokenExpired(handleAccessTokenExpired);
      manager.events.removeAccessTokenExpiring(handleAccessTokenExpiring);
      manager.events.removeSilentRenewError(handleSilentRenewError);
    };
  }, []);

  const login = async (returnUrl = '/') => {
    if (userManager) {
      try {
        await userManager.signinRedirect({ 
          state: { returnUrl } 
        });
      } catch (error) {
        console.error('Login failed:', error);
      }
    }
  };

const logout = async () => {
  if (userManager) {
    try {
      // Clear local state immediately
      setUser(null);
      setIsAuthenticated(false);
      
      // Clear any stored tokens
      await userManager.removeUser();
      
      // Then redirect to identity server for complete logout
      await userManager.signoutRedirect({
        post_logout_redirect_uri: AUTH_URLS.LOGOUT_REDIRECT
      });
      console.log('Logout successful');
    } catch (error) {
      console.error('Logout failed:', error);
      // Ensure local state is cleared even if logout fails
      setUser(null);
      setIsAuthenticated(false);
    }
  }
};

  const getAccessToken = () => {
    return user?.access_token || null;
  };

  const getIdToken = () => {
    return user?.id_token || null;
  };

  const getUserClaims = () => {
    return user?.profile || null;
  };

  const value = {
    user,
    isAuthenticated,
    isLoading,
    userManager,
    login,
    logout,
    getAccessToken,
    getIdToken,
    getUserClaims,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};