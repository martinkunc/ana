import React, { useState, useEffect } from 'react';
import { useApiClient } from '../hooks/useApiClient';
import { useAuth } from '../contexts/AuthContext';
import { AnaGroupMember } from '../services/ApiClient';

interface NewUser {
  email: string;
}

const Members: React.FC = () => {
  const { user, isAuthenticated } = useAuth();
  const apiClient = useApiClient();
  
  const [groupMembers, setGroupMembers] = useState<AnaGroupMember[]>([]);
  const [membersLoadingStatus, setMembersLoadingStatus] = useState<string | null>('Loading...');
  const [membersOfGroupTitle, setMembersOfGroupTitle] = useState<string>('Members');
  const [displayedGroupId, setDisplayedGroupId] = useState<string>('');
  const [isAdmin, setIsAdmin] = useState<boolean>(false);
  const [newUser, setNewUser] = useState<NewUser>({ email: '' });
  const [addMemberStatusMessage, setAddMemberStatusMessage] = useState<string>('');
  const [addMemberStatusColor, setAddMemberStatusColor] = useState<string>('black');
  const [errors, setErrors] = useState<{ [key: string]: string }>({});

  useEffect(() => {
    if (isAuthenticated && user) {
      loadMembers();
    }
  }, [isAuthenticated, user]);

  const loadMembers = async () => {
    try {
      setMembersLoadingStatus('Loading...');
      
      const userId = user.profile?.sub;
      if (!userId) {
        setMembersLoadingStatus('User not authenticated');
        return;
      }

      const selectedGroupResponse = await apiClient.getUserSelectedGroup(userId);
      const userGroupId = selectedGroupResponse.anaGroup.id;
      const userRole = selectedGroupResponse.userRole;
      
      if (!userGroupId) {
        setMembersLoadingStatus('No group found for user');
        return;
      }

      setDisplayedGroupId(userGroupId);
      setMembersOfGroupTitle(`Members of ${selectedGroupResponse.anaGroup.name}`);
      setIsAdmin(userRole === 'Admin');

      const members = await apiClient.getGroupMembers(userGroupId);
      setGroupMembers(members);
      setMembersLoadingStatus(null);
      
    } catch (error) {
      console.error('Error loading members:', error);
      setMembersLoadingStatus('Error loading members');
    }
  };

  const validateForm = (): boolean => {
    const newErrors: { [key: string]: string } = {};

    if (!newUser.email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(newUser.email)) {
      newErrors.email = 'Please enter a valid email address';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const addGroupMember = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    try {
      setAddMemberStatusMessage('');
      
      await apiClient.createGroupMember(displayedGroupId, {
        email: newUser.email,
        role: 'Member' // Default role
      });
      
      setAddMemberStatusMessage('Member added successfully');
      setAddMemberStatusColor('green');
      setNewUser({ email: '' });
      setErrors({});
      
      // Reload members to show the new member
      await loadMembers();
      
    } catch (error) {
      console.error('Error adding member:', error);
      setAddMemberStatusMessage(`Error adding member: ${error}`);
      setAddMemberStatusColor('red');
    }
  };

  const checkAdminChanged = async (isChecked: boolean, userId: string, groupId: string) => {
    try {
      const newRole = isChecked ? 'Admin' : 'Member';
      
      await apiClient.changeGroupMemberRole(groupId, userId, {
        role: newRole
      });
      
      // Update the local state
      setGroupMembers(prev => prev.map(member => 
        member.userId === userId 
          ? { ...member, role: newRole }
          : member
      ));
      
    } catch (error) {
      console.error('Error changing member role:', error);
      // Reload members to revert changes
      await loadMembers();
    }
  };

  const removeMember = async (groupId: string, userId: string) => {
    if (!window.confirm('Are you sure you want to remove this member from the group?')) {
      return;
    }

    try {
      await apiClient.deleteGroupMember(groupId, userId);
      
      // Remove from local state
      setGroupMembers(prev => prev.filter(member => member.userId !== userId));
      
    } catch (error) {
      console.error('Error removing member:', error);
      // Reload members in case of error
      await loadMembers();
    }
  };

  if (!isAuthenticated) {
    return <div>Please log in to view members.</div>;
  }

  return (
    <div>
      <h1>{membersOfGroupTitle}</h1>

      {membersLoadingStatus ? (
        <p><em>{membersLoadingStatus}</em></p>
      ) : (
        <table style={{ width: '100%', tableLayout: 'fixed' }}>
          <colgroup>
            <col style={{ width: 'auto' }} />
            {isAdmin && <col style={{ width: '20em' }} />}
          </colgroup>
          <tbody>
            {groupMembers.map((m) => (
              <tr key={m.userId}>
                <td>{m.displayName}</td>
                {isAdmin && (
                  <td style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <div>
                      <input 
                        type="checkbox" 
                        checked={m.role === 'Admin'}
                        onChange={(e) => checkAdminChanged(e.target.checked, m.userId, displayedGroupId)}
                      />
                      Administrator
                    </div>
                    <button 
                      type="button" 
                      onClick={() => removeMember(displayedGroupId, m.userId)}
                    >
                      Remove
                    </button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {isAdmin && (
        <div style={{ marginTop: '2em' }}>
          <div>Add user to group</div>
          <div style={{ display: 'flex', gap: '0.5em', marginTop: '0.5em' }}>
            <span style={{ flex: 1 }}>Existing Anniversary Notification application user's Email</span>
          </div>
          
          <form onSubmit={addGroupMember}>
            <div style={{ display: 'flex', gap: '0.5em', marginTop: '0.5em' }}>
              <input
                type="email"
                style={{ flex: 1 }}
                value={newUser.email}
                onChange={(e) => setNewUser({ email: e.target.value })}
                placeholder="user@domain.com"
              />
            </div>
            
            {/* Validation errors */}
            {Object.keys(errors).length > 0 && (
              <div style={{ color: 'red', marginTop: '0.5em' }}>
                {Object.values(errors).map((error, index) => (
                  <div key={index}>{error}</div>
                ))}
              </div>
            )}
            
            <div style={{ marginTop: '0.5em' }}>
              <button type="submit">Add</button>
            </div>
            
            {addMemberStatusMessage && (
              <div style={{ color: addMemberStatusColor, marginTop: '0.5em' }}>
                {addMemberStatusMessage}
              </div>
            )}
          </form>
        </div>
      )}
    </div>
  );
};

export default Members;