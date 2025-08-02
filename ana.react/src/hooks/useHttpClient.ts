import { useMemo } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { HttpClientService } from '../services/HttpClientService';
import { PUBLIC_URLS } from '../config/config';

export const useHttpClient = () => {
  const { getAccessToken } = useAuth();

  const httpClient = useMemo(() => {
    const getToken = (): string | null => {
      try {
        return getAccessToken();
      } catch (error) {
        console.error('Failed to get access token:', error);
        return null;
      }
    };

    console.log('Creating HttpClient with base URL:', PUBLIC_URLS.API_BASE);
    return new HttpClientService(PUBLIC_URLS.API_BASE, getToken);
  }, [getAccessToken]);

  return httpClient;
};
