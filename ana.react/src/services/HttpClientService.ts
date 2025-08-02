export interface ApiResponse<T = any> {
  data?: T;
  error?: string;
  status: number;
  success: boolean;
}

export class HttpClientService {
  private baseUrl: string;
  private getAccessToken: () => string | null;

  constructor(baseUrl: string, getAccessToken: () => string | null) {
    this.baseUrl = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
    this.getAccessToken = getAccessToken;
  }

  private async getAuthHeaders(): Promise<HeadersInit> {
    const token = this.getAccessToken();
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
      console.log('HttpClient configured with valid token');
    } else {
      console.warn('No access token available');
    }

    return headers;
  }

  private async handleResponse<T>(response: Response): Promise<ApiResponse<T>> {
    const success = response.ok;
    const status = response.status;

    try {
      let data: T | undefined;
      const contentType = response.headers.get('content-type');
      
      if (contentType && contentType.includes('application/json')) {
        data = await response.json();
      } else if (response.status !== 204) { // No Content
        data = (await response.text()) as unknown as T;
      }
      
      if (!success) {
        const errorMessage = typeof data === 'object' && data && 'message' in data 
          ? (data as any).message 
          : typeof data === 'string' 
            ? data 
            : `HTTP ${status}`;
        
        console.error(`API Error ${status}:`, data);
        return {
          data: undefined,
          error: errorMessage,
          status,
          success: false
        };
      }

      return {
        data,
        error: undefined,
        status,
        success: true
      };
    } catch (error) {
      console.error('Failed to parse response:', error);
      return {
        data: undefined,
        error: `Failed to parse response: ${error}`,
        status,
        success: false
      };
    }
  }

  async get<T>(endpoint: string): Promise<ApiResponse<T>> {
    try {
      const url = `${this.baseUrl}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;
      const headers = await this.getAuthHeaders();

      console.log(`GET ${url}`);
      
      const response = await fetch(url, {
        method: 'GET',
        headers,
      });

      return this.handleResponse<T>(response);
    } catch (error) {
      console.error('GET request failed:', error);
      return {
        data: undefined,
        error: `Network error: ${error}`,
        status: 0,
        success: false
      };
    }
  }

  async post<T>(endpoint: string, data?: any): Promise<ApiResponse<T>> {
    try {
      const url = `${this.baseUrl}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;
      const headers = await this.getAuthHeaders();

      console.log(`POST ${url}`, data);

      const response = await fetch(url, {
        method: 'POST',
        headers,
        body: data ? JSON.stringify(data) : undefined,
      });

      return this.handleResponse<T>(response);
    } catch (error) {
      console.error('POST request failed:', error);
      return {
        data: undefined,
        error: `Network error: ${error}`,
        status: 0,
        success: false
      };
    }
  }

  async put<T>(endpoint: string, data?: any): Promise<ApiResponse<T>> {
    try {
      const url = `${this.baseUrl}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;
      const headers = await this.getAuthHeaders();

      console.log(`PUT ${url}`, data);

      const response = await fetch(url, {
        method: 'PUT',
        headers,
        body: data ? JSON.stringify(data) : undefined,
      });

      return this.handleResponse<T>(response);
    } catch (error) {
      console.error('PUT request failed:', error);
      return {
        data: undefined,
        error: `Network error: ${error}`,
        status: 0,
        success: false
      };
    }
  }

  async delete<T>(endpoint: string): Promise<ApiResponse<T>> {
    try {
      const url = `${this.baseUrl}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;
      const headers = await this.getAuthHeaders();

      console.log(`DELETE ${url}`);

      const response = await fetch(url, {
        method: 'DELETE',
        headers,
      });

      return this.handleResponse<T>(response);
    } catch (error) {
      console.error('DELETE request failed:', error);
      return {
        data: undefined,
        error: `Network error: ${error}`,
        status: 0,
        success: false
      };
    }
  }
}
