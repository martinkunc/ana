import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';
import { useAuth } from './AuthContext';
import { useApiClient } from '../hooks/useApiClient';
import { AnaGroup } from '../services/ApiClient';
// Provider for user display name and selected group
interface SelectedGroupContextType {
  selectedGroup: AnaGroup | null;
  anaGroupName: string;
  refreshSelectedGroup: () => Promise<void>;
  isLoading: boolean;
  displayName: string;
  updateDisplayName: (newDisplayName: string) => void;
  refreshDisplayName: () => Promise<void>;
  isLoadingDisplayName: boolean;
}

const SharedStateContext = createContext<SelectedGroupContextType | undefined>(undefined);

export const useSharedState = () => {
  const context = useContext(SharedStateContext);
  if (!context) {
    throw new Error('useSharedState must be used within a SharedStateProvider');
  }
  return context;
};

export const SharedStateProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [selectedGroup, setSelectedGroup] = useState<AnaGroup | null>(null);
  const [anaGroupName, setAnaGroupName] = useState<string>('Loading...');
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [displayName, setDisplayName] = useState<string>('');
  const [isLoadingDisplayName, setIsLoadingDisplayName] = useState<boolean>(false);
  const { user, isAuthenticated } = useAuth();
  const apiClient = useApiClient();

  const refreshDisplayName = useCallback(async () => {
    if (!isAuthenticated || !user) {
      setDisplayName('');
      return;
    }

    setIsLoadingDisplayName(true);
    
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
      setIsLoadingDisplayName(false);
    }
  }, [isAuthenticated, user, apiClient]);

  const updateDisplayName = useCallback((newDisplayName: string) => {
    setDisplayName(newDisplayName);
  }, []);

  const refreshSelectedGroup = useCallback(async () => {
    if (!isAuthenticated || !user) {
      console.log('User not authenticated, skipping group refresh');
      setSelectedGroup(null);
      setAnaGroupName('Not authenticated');
      return;
    }

    try {
      setIsLoading(true);
      console.log('Refreshing selected group');
      
      const userId = user.profile?.sub;
      if (!userId) {
        console.error('User ID not found in claims');
        setAnaGroupName('No user ID');
        setSelectedGroup(null);
        return;
      }

      console.log(`Fetching selected group for user: ${userId}`);
      
      const selectedGroupResponse = await apiClient.getUserSelectedGroup(userId);
      if (selectedGroupResponse?.anaGroup) {
        setSelectedGroup(selectedGroupResponse.anaGroup);
        setAnaGroupName(selectedGroupResponse.anaGroup.name);
        console.log(`Set group name to: ${selectedGroupResponse.anaGroup.name}`);
      } else {
        setSelectedGroup(null);
        setAnaGroupName('No group selected');
      }
    } catch (error) {
      console.error('Error refreshing selected group:', error);
      setAnaGroupName('Error loading');
      setSelectedGroup(null);
    } finally {
      setIsLoading(false);
    }
  }, [isAuthenticated, user, apiClient]);

  useEffect(() => {
    refreshSelectedGroup();
    refreshDisplayName();
  }, [refreshSelectedGroup, refreshDisplayName]);

  const value = {
    selectedGroup,
    anaGroupName,
    refreshSelectedGroup,
    isLoading,
    displayName,
    updateDisplayName,
    refreshDisplayName,
    isLoadingDisplayName
  };

  return (
    <SharedStateContext.Provider value={value}>
      {children}
    </SharedStateContext.Provider>
  );
};
