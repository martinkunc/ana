export interface AnaGroup { // ok
  id: string;
  name: string;
  description?: string;
}

export interface AnaUser { // ok
  id: string;
  displayName: string;
  selectedGroupId?: string;
  preferredNotification: string;
  whatsAppNumber:string;
}

export interface AnaAnniv { // ok
  id?: string;
  groupId?: string;
  name?: string;
  date?: string;
  alignedDate?: string;
}

export interface SelectedGroupResponse { // ok
  anaGroup: AnaGroup;
  userRole: string;
}

export interface AnaGroupMember { // ok
  userId: string;
  groupId: string;
  role: string;
  email: string;
  displayName: string;
}

export class ApiClient {
  private baseUrl: string;
  private getAccessToken: () => string | null;

  constructor(baseUrl: string, getAccessToken: () => string | null) {
    this.baseUrl = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
    this.getAccessToken = getAccessToken;
  }

  private async makeRequest<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${this.baseUrl}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;
    const token = this.getAccessToken();

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(options.headers as Record<string, string>),
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    console.log(`${options.method || 'GET'} ${url}`);

    const response = await fetch(url, {
      ...options,
      headers,
    });

    if (!response.ok) {
      let errorMessage = `HTTP ${response.status}: ${response.statusText}`;
      
      try {
        const errorText = await response.text();
        if (errorText) {
          const errorData = JSON.parse(errorText);
          if (typeof errorData === 'object' && errorData.message) {
            errorMessage = errorData.message;
          } else if (typeof errorData === 'string') {
            errorMessage = errorData;
          }
        }
      } catch {
        // Keep the default error message if we can't parse the error response
      }
      
      console.error(`API Error ${response.status}:`, errorMessage);
      throw new Error(errorMessage);
    }

    // Handle empty responses (204 No Content, etc.)
    if (response.status === 204 || response.headers.get('content-length') === '0') {
      return undefined as T;
    }

    // Parse JSON response
    const text = await response.text();
    if (!text) {
      return undefined as T;
    }

    try {
      return JSON.parse(text) as T;
    } catch (parseError) {
      console.error('Failed to parse JSON response:', parseError);
      throw new Error('Invalid JSON response from server');
    }
  }

  // User methods
  async getUserGroups(userId: string): Promise<AnaGroup[]> {
    return this.makeRequest<AnaGroup[]>(`/api/v1/user/groups/${userId}`);
  }

  async getUserSelectedGroup(userId: string): Promise<SelectedGroupResponse> {
    return this.makeRequest<SelectedGroupResponse>(`/api/v1/user/select-group/${userId}`);
  }

  async selectUserGroup(userId: string, groupId: string): Promise<void> {
    return this.makeRequest<void>(`/api/v1/user/select-group/${userId}/${groupId}`, {
      method: 'POST'
    });
  }

  async createUser(userData: any): Promise<void> {
    return this.makeRequest<void>('/api/v1/user', {
      method: 'POST',
      body: JSON.stringify(userData)
    });
  }

  async getUserSettings(userId: string): Promise<AnaUser> {
    return this.makeRequest<AnaUser>(`/api/v1/user/${userId}`);
  }

  async updateUserSettings(userId: string, settings: any): Promise<void> {
    return this.makeRequest<void>(`/api/v1/user/${userId}`, {
      method: 'PUT',
      body: JSON.stringify(settings)
    });
  }

  async deleteUser(userId: string): Promise<void> {
    return this.makeRequest<void>(`/api/v1/user/${userId}`, {
      method: 'DELETE'
    });
  }

  // Group methods
  async createGroup(group: any): Promise<AnaGroup> {
    return this.makeRequest<AnaGroup>('/api/v1/group', {
      method: 'POST',
      body: JSON.stringify(group)
    });
  }

  async getGroupMembers(groupId: string): Promise<AnaGroupMember[]> {
    return this.makeRequest<AnaGroupMember[]>(`/api/v1/group/${groupId}/members`);
  }

  async createGroupMember(groupId: string, memberData: any): Promise<void> {
    return this.makeRequest<void>(`/api/v1/group/${groupId}/member`, {
      method: 'POST',
      body: JSON.stringify(memberData)
    });
  }

  async changeGroupMemberRole(groupId: string, userId: string, roleData: any): Promise<void> {
    return this.makeRequest<void>(`/api/v1/group/${groupId}/member/${userId}/role`, {
      method: 'PUT',
      body: JSON.stringify(roleData)
    });
  }

  async deleteGroupMember(groupId: string, userId: string): Promise<void> {
    return this.makeRequest<void>(`/api/v1/group/${groupId}/member/${userId}`, {
      method: 'DELETE'
    });
  }

  // Anniversary methods
  async getAnniversaries(groupId: string): Promise<AnaAnniv[]> {
    return this.makeRequest<AnaAnniv[]>(`/api/v1/group/${groupId}/anniversaries`);
  }

  async createAnniversary(groupId: string, anniversary: Partial<AnaAnniv>): Promise<void> {
    return this.makeRequest<void>(`/api/v1/group/${groupId}/anniversary`, {
      method: 'POST',
      body: JSON.stringify(anniversary)
    });
  }

  async updateAnniversary(groupId: string, anniversaryId: string, anniversary: Partial<AnaAnniv>): Promise<void> {
    return this.makeRequest<void>(`/api/v1/group/${groupId}/anniversary/${anniversaryId}`, {
      method: 'PUT',
      body: JSON.stringify(anniversary)
    });
  }

  async deleteAnniversary(groupId: string, anniversaryId: string): Promise<void> {
    return this.makeRequest<void>(`/api/v1/group/${groupId}/anniversary/${anniversaryId}`, {
      method: 'DELETE'
    });
  }

  // Utility methods
  async runDailyTask(): Promise<void> {
    return this.makeRequest<void>('/api/v1/daily-task', {
      method: 'POST'
    });
  }

  // Alias methods for consistency
  async addGroupMember(groupId: string, memberData: any): Promise<void> {
    return this.createGroupMember(groupId, memberData);
  }

  async removeGroupMember(groupId: string, userId: string): Promise<void> {
    return this.deleteGroupMember(groupId, userId);
  }
}