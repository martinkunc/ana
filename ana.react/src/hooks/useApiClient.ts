import { useMemo } from 'react';
import { ApiClient } from '../services/ApiClient';
import { PUBLIC_URLS } from '../config/config';
import { useAuth } from '../contexts/AuthContext';

export const useApiClient = () => {
  const { getAccessToken } = useAuth();
  const apiClient = useMemo(() => {
    return new ApiClient(PUBLIC_URLS.API_BASE, getAccessToken);
  }, [getAccessToken]);

  return apiClient;
};
