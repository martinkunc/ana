import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';
import { useAuth } from './AuthContext';
import { useApiClient } from '../hooks/useApiClient';
import { AnaGroup } from '../services/ApiClient';

interface SelectedGroupContextType {
  selectedGroup: AnaGroup | null;
  anaGroupName: string;
  refreshSelectedGroup: () => Promise<void>;
  isLoading: boolean;
}

const SelectedGroupContext = createContext<SelectedGroupContextType | undefined>(undefined);

export const useSelectedGroup = () => {
  const context = useContext(SelectedGroupContext);
  if (!context) {
    throw new Error('useSelectedGroup must be used within a SelectedGroupProvider');
  }
  return context;
};

export const SelectedGroupProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [selectedGroup, setSelectedGroup] = useState<AnaGroup | null>(null);
  const [anaGroupName, setAnaGroupName] = useState<string>('Loading...');
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const { user, isAuthenticated } = useAuth();
  const apiClient = useApiClient();

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
  }, [refreshSelectedGroup]);

  const value = {
    selectedGroup,
    anaGroupName,
    refreshSelectedGroup,
    isLoading
  };

  return (
    <SelectedGroupContext.Provider value={value}>
      {children}
    </SelectedGroupContext.Provider>
  );
};
