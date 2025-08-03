import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { useApiClient } from './useApiClient';

export const useUserDisplayName = () => {
  const { user, isAuthenticated } = useAuth();
  const apiClient = useApiClient();
  const [displayName, setDisplayName] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    const fetchDisplayName = async () => {
      if (!isAuthenticated || !user) {
        setDisplayName('');
        return;
      }

      setIsLoading(true);
      
      try {
        const userId = user.profile?.sub;
        if (userId) {
          const anaUser = await apiClient.getUserSettings(userId);
          setDisplayName(anaUser.displayName);
        }
      } catch (error) {
        console.error('Failed to fetch display name:', error);
        
        const fallbackName = user.profile?.name || 
                            user.profile?.preferred_username || 
                            user.profile?.email || 
                            'User';
        setDisplayName(fallbackName);
      } finally {
        setIsLoading(false);
      }
    };

    fetchDisplayName();
  }, [isAuthenticated, user, apiClient]);

  return { displayName, isLoading };
};