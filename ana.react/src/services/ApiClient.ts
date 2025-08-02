import { HttpClientService, ApiResponse } from '../services/HttpClientService';

export interface AnaGroup {
  id: string;
  name: string;
  description?: string;
}

export interface AnaUser {
  id: string;
  email: string;
  name: string;
  selectedGroupId?: string;
}

export interface Anniversary {
  id: string;
  title: string;
  date: string;
  description?: string;
  groupId: string;
}

export interface SelectedGroupResponse {
  anaGroup: AnaGroup;
}

export interface GroupMember {
  userId: string;
  userName: string;
  email: string;
  role?: string;
}

export interface NotificationSettings {
  emailNotifications: boolean;
  reminderDays: number;
}

export class ApiClient {
  constructor(private httpClient: HttpClientService) {}

  async getUserGroups(userId: string): Promise<ApiResponse<any>> {
    return this.httpClient.get(`/api/v1/user/groups/${userId}`);
  }

  async getUserSelectedGroup(userId: string): Promise<ApiResponse<SelectedGroupResponse>> {
    return this.httpClient.get<SelectedGroupResponse>(`/api/v1/user/select-group/${userId}`);
  }

  async selectUserGroup(userId: string, groupId: string): Promise<ApiResponse<void>> {
    return this.httpClient.post<void>(`/api/v1/user/select-group/${userId}/${groupId}`);
  }

  async createUser(userData: any): Promise<ApiResponse<void>> {
    return this.httpClient.post<void>('/api/v1/user', userData);
  }

  async getUserSettings(userId: string): Promise<ApiResponse<any>> {
    return this.httpClient.get(`/api/v1/user/${userId}`);
  }

  async updateUserSettings(userId: string, settings: any): Promise<ApiResponse<void>> {
    return this.httpClient.put<void>(`/api/v1/user/${userId}`, settings);
  }

  async deleteUser(userId: string): Promise<ApiResponse<void>> {
    return this.httpClient.delete<void>(`/api/v1/user/${userId}`);
  }

  async createGroup(group: any): Promise<ApiResponse<any>> {
    return this.httpClient.post('/api/v1/group', group);
  }

  async getGroupMembers(groupId: string): Promise<ApiResponse<GroupMember[]>> {
    return this.httpClient.get<GroupMember[]>(`/api/v1/group/${groupId}/members`);
  }

  async createGroupMember(groupId: string, memberData: any): Promise<ApiResponse<void>> {
    return this.httpClient.post<void>(`/api/v1/group/${groupId}/member`, memberData);
  }

  async changeGroupMemberRole(groupId: string, userId: string, roleData: any): Promise<ApiResponse<void>> {
    return this.httpClient.put<void>(`/api/v1/group/${groupId}/member/${userId}/role`, roleData);
  }

  async deleteGroupMember(groupId: string, userId: string): Promise<ApiResponse<void>> {
    return this.httpClient.delete<void>(`/api/v1/group/${groupId}/member/${userId}`);
  }

  async getGroupAnniversaries(groupId: string): Promise<ApiResponse<Anniversary[]>> {
    return this.httpClient.get<Anniversary[]>(`/api/v1/group/${groupId}/anniversaries`);
  }

  async createAnniversary(groupId: string, anniversary: Partial<Anniversary>): Promise<ApiResponse<void>> {
    return this.httpClient.post<void>(`/api/v1/group/${groupId}/anniversary`, anniversary);
  }

  async updateAnniversary(groupId: string, anniversaryId: string, anniversary: Partial<Anniversary>): Promise<ApiResponse<void>> {
    return this.httpClient.put<void>(`/api/v1/group/${groupId}/anniversary/${anniversaryId}`, anniversary);
  }

  async deleteAnniversary(groupId: string, anniversaryId: string): Promise<ApiResponse<void>> {
    return this.httpClient.delete<void>(`/api/v1/group/${groupId}/anniversary/${anniversaryId}`);
  }

  async runDailyTask(): Promise<ApiResponse<void>> {
    return this.httpClient.post<void>('/api/v1/daily-task');
  }

  async getAnniversaries(groupId?: string): Promise<ApiResponse<Anniversary[]>> {
    if (groupId) {
      return this.getGroupAnniversaries(groupId);
    }
    throw new Error('Group ID is required for getting anniversaries');
  }

  async addGroupMember(groupId: string, memberData: any): Promise<ApiResponse<void>> {
    return this.createGroupMember(groupId, memberData);
  }

  async removeGroupMember(groupId: string, userId: string): Promise<ApiResponse<void>> {
    return this.deleteGroupMember(groupId, userId);
  }
}
