import React, { useState, useEffect } from 'react';
import { useApiClient } from '../hooks/useApiClient';
import { useAuth } from '../contexts/AuthContext';
import { useSharedState } from '../contexts/SharedStateContext';
import { AnaGroup } from '../services/ApiClient';
import styles from './MyGroups.module.css';

interface NewGroup {
  userId: string;
  name: string;
}

const MyGroups: React.FC = () => {
  const { user, isAuthenticated } = useAuth();
  const apiClient = useApiClient();
  const { selectedGroup, refreshSelectedGroup } = useSharedState();
  
  const [myGroupsList, setMyGroupsList] = useState<AnaGroup[]>([]);
  const [groupsLoadingStatus, setGroupsLoadingStatus] = useState<string | null>('Loading...');
  const [displayedUserId, setDisplayedUserId] = useState<string>('');
  const [newGroup, setNewGroup] = useState<NewGroup>({
    userId: '',
    name: ''
  });
  const [addGroupStatusMessage, setAddGroupStatusMessage] = useState<string>('');
  const [errors, setErrors] = useState<{ [key: string]: string }>({});

  useEffect(() => {
    if (isAuthenticated && user) {
      initializeComponent();
    }
  }, [isAuthenticated, user]);

  const initializeComponent = async () => {
    try {
      const userId = user?.profile?.sub;
      if (!userId) {
        setGroupsLoadingStatus('User not authenticated');
        return;
      }
      setDisplayedUserId(userId);
     
      setNewGroup({ userId, name: '' });
      
      await refreshGroups(userId);
    } catch (error) {
      console.error('Error initializing MyGroups:', error);
      setGroupsLoadingStatus('Error loading groups');
    }
  };

  const refreshGroups = async (userId: string) => {
    try {
      setGroupsLoadingStatus('Loading...');
      
      const groups = await apiClient.getUserGroups(userId);
      setMyGroupsList(groups);
      
      if (!groups || groups.length === 0) {
        setGroupsLoadingStatus('No groups found.');
      } else {
        setGroupsLoadingStatus(null);
      }
      
    } catch (error) {
      console.error('Error loading groups:', error);
      setGroupsLoadingStatus('Error loading groups');
      setMyGroupsList([]);
    }
  };

  const validateForm = (): boolean => {
    const newErrors: { [key: string]: string } = {};

    if (!newGroup.name.trim()) {
      newErrors.name = 'Group name is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const addGroup = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    try {
      setAddGroupStatusMessage('');
      
      await apiClient.createGroup({ 
        userId: newGroup.userId, 
        name: newGroup.name 
      });
      
      console.log(`Added group: ${newGroup.name}`);
      
      setAddGroupStatusMessage('Group added successfully!');
      await refreshGroups(newGroup.userId);
      
      setNewGroup({ userId: newGroup.userId, name: '' });
      setErrors({});
      
    } catch (error) {
      console.error('Error adding group:', error);
      setAddGroupStatusMessage('Failed to add group');
    }
  };

  const switchGroup = async (groupId: string, userId: string) => {
    try {
      console.log(`Switching group to ${groupId} for userId ${userId}`);
      
      await apiClient.selectUserGroup(userId, groupId);
      console.log('Group selection changed');
      
      await refreshSelectedGroup();
      
      setNewGroup({ userId, name: '' });
      await refreshGroups(userId);
      
    } catch (error) {
      console.error('Error switching group:', error);
      setAddGroupStatusMessage('Failed to switch group');
    }
  };

  if (!isAuthenticated) {
    return <div>Please log in to view your groups.</div>;
  }

  return (
    <div>
      <h1>My other groups</h1>
      
      {groupsLoadingStatus ? (
        <p><em>{groupsLoadingStatus}</em></p>
      ) : (
        <table className={styles.groupsTable}>
          <colgroup>
            <col style={{ width: '20em' }} />
            <col style={{ width: 'auto' }} />
          </colgroup>
          <tbody>
            {myGroupsList.map((group) => (
              <tr key={group.id}>
                <td className={styles.actionCell}>
                  {group.id !== selectedGroup?.id && (
                    <div>
                      <button 
                        type="button"
                        onClick={() => switchGroup(group.id, displayedUserId)}
                      >
                        Switch
                      </button>
                    </div>
                  )}
                </td>
                <td>{group.name}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <div className={styles.createGroupSection}>
        <div>Create a new group</div>
        <form onSubmit={addGroup} className={styles.createGroupForm}>
          <div className={styles.inputContainer}>
            <input
              type="text"
              value={newGroup.name}
              onChange={(e) => setNewGroup(prev => ({ ...prev, name: e.target.value }))}
              className={styles.groupNameInput}
              placeholder="Group name"
            />
            {errors.name && <div className={styles.error}>{errors.name}</div>}
          </div>
          <div className={styles.submitContainer}>
            <button type="submit">
              Create
            </button>
          </div>
          {addGroupStatusMessage && (
            <div className={styles.statusMessage}>
              {addGroupStatusMessage}
            </div>
          )}
        </form>
      </div>
    </div>
  );
};

export default MyGroups;
