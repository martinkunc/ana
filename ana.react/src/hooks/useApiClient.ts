import { useMemo } from 'react';
import { useHttpClient } from './useHttpClient';
import { ApiClient } from '../services/ApiClient';


export const useApiClient = () => {
  const httpClient = useHttpClient();

  const apiClient = useMemo(() => {
    return new ApiClient(httpClient);
  }, [httpClient]);

  return apiClient;
};
