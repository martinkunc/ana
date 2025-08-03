import React, { useState, useEffect } from 'react';
import { useApiClient } from '../hooks/useApiClient';
import { useAuth } from '../contexts/AuthContext';
import { AnaAnniv } from '../services/ApiClient';

interface NewAnniversary {
  id?: string;
  groupId: string;
  date: string;
  name: string;
}

const Home: React.FC = () => {
  const { user, isAuthenticated } = useAuth();
  const apiClient = useApiClient();
  
  const [anniversaries, setAnniversaries] = useState<AnaAnniv[]>([]);
  const [anniversariesLoadingStatus, setAnniversariesLoadingStatus] = useState<string | null>('Loading...');
  const [newAnniversary, setNewAnniversary] = useState<NewAnniversary>({
    groupId: '',
    date: '',
    name: ''
  });
  const [errors, setErrors] = useState<{ [key: string]: string }>({});

  useEffect(() => {
    if (isAuthenticated && user) {
      loadAnniversaries();
    }
  }, [isAuthenticated, user]);

  const loadAnniversaries = async () => {
    try {
      setAnniversariesLoadingStatus('Loading...');
      
      const userId = user.profile?.sub;
      if (!userId) {
        setAnniversariesLoadingStatus('User not authenticated');
        return;
      }

      const selectedGroupResponse = await apiClient.getUserSelectedGroup(userId);
      const userGroupId = selectedGroupResponse?.anaGroup?.id;
      
      if (!userGroupId) {
        setAnniversariesLoadingStatus('No group found for user');
        return;
      }

      const anniversaries = await apiClient.getAnniversaries(userGroupId);
      setAnniversaries(anniversaries);
      setAnniversariesLoadingStatus(null);
      setNewAnniversary(prev => ({ ...prev, groupId: userGroupId }));
      
    } catch (error) {
      console.error('Error loading anniversaries:', error);
      setAnniversariesLoadingStatus('Error loading anniversaries');
    }
  };

  const validateForm = (): boolean => {
    const newErrors: { [key: string]: string } = {};

    if (!newAnniversary.date.trim()) {
      newErrors.date = 'Date is required';
    } else if (!/^(0?[1-9]|[12][0-9]|3[01])\/(0?[1-9]|1[0-2])$/.test(newAnniversary.date)) {
      newErrors.date = 'Date must be in d/m format';
    }

    if (!newAnniversary.name.trim()) {
      newErrors.name = 'Occasion name is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const addAnniversary = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    try {
      if (newAnniversary.id) {
        // Update existing anniversary
        await apiClient.updateAnniversary(
          newAnniversary.groupId,
          newAnniversary.id,
          {
            id : newAnniversary.id,
            date: newAnniversary.date,
            name: newAnniversary.name,
            groupId: newAnniversary.groupId
          }
        );
      } else {
        // Add new anniversary
        await apiClient.createAnniversary(newAnniversary.groupId, {
          date: newAnniversary.date,
          name: newAnniversary.name,
          groupId: newAnniversary.groupId
        });
      }
      
      await loadAnniversaries();
      resetForm();
    } catch (error) {
      console.error('Error saving anniversary:', error);
    }
  };

  const editAnniversary = (id: string, groupId: string, date: string, name: string) => {
    setNewAnniversary({
      id,
      groupId,
      date,
      name
    });
    setErrors({});
  };

  const removeAnniversary = async (id: string, groupId: string) => {
    if (!window.confirm('Are you sure you want to remove this anniversary?')) {
      return;
    }

    try {
      await apiClient.deleteAnniversary(groupId, id);
      await loadAnniversaries();
      
      // If we're editing the anniversary that was deleted, reset the form
      if (newAnniversary.id === id) {
        resetForm();
      }
    } catch (error) {
      console.error('Error removing anniversary:', error);
    }
  };

  const resetForm = () => {
    setNewAnniversary({
      groupId: newAnniversary.groupId, // Keep the groupId
      date: '',
      name: ''
    });
    setErrors({});
  };

  if (!isAuthenticated) {
    return <div>Please log in to view anniversaries.</div>;
  }

  return (
    <div>
      <h1>Anniversaries</h1>

      {anniversariesLoadingStatus ? (
        <p><em>{anniversariesLoadingStatus}</em></p>
      ) : (
        <table style={{ width: '100%', tableLayout: 'fixed' }}>
          <colgroup>
            <col style={{ width: '9em' }} />
            <col style={{ width: 'auto' }} />
            <col style={{ width: '9em' }} />
          </colgroup>
          <tbody>
            {anniversaries.map((a) => (
              <tr key={a.id}>
                <td>{a.date}</td>
                <td>{a.name}</td>
                <td>
                  <button 
                    type="button" 
                    onClick={() => editAnniversary(a.id!, a.groupId!, a.date!, a.name!)}
                    style={{ marginRight: '0.5em' }}
                  >
                    Edit
                  </button>
                  <button 
                    type="button" 
                    onClick={() => removeAnniversary(a.id!, a.groupId!)}
                  >
                    Remove
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <div style={{ marginTop: '2em' }}>
        <div>
          {!newAnniversary.id ? 'Add new anniversary' : 'Editing existing anniversary'}
        </div>
        <div style={{ display: 'flex', gap: '0.5em', marginTop: '0.5em' }}>
          <span style={{ width: '9em' }}>Date (day/month)</span>
          <span style={{ flex: 1 }}>Occasion</span>
        </div>
        
        <form onSubmit={addAnniversary}>
          <div style={{ display: 'flex', gap: '0.5em', marginTop: '0.5em' }}>
            <input
              type="text"
              style={{ width: '9em' }}
              value={newAnniversary.date}
              onChange={(e) => setNewAnniversary(prev => ({ ...prev, date: e.target.value }))}
              placeholder="d/m"
            />
            <input
              type="text"
              style={{ flex: 1 }}
              value={newAnniversary.name}
              onChange={(e) => setNewAnniversary(prev => ({ ...prev, name: e.target.value }))}
              placeholder="name"
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
            <button type="submit">
              {!newAnniversary.id ? 'Add' : 'Save'}
            </button>
            {newAnniversary.id && (
              <button 
                type="button" 
                onClick={resetForm}
                style={{ marginLeft: '0.5em' }}
              >
                Cancel
              </button>
            )}
          </div>
        </form>
      </div>
    </div>
  );
};

export default Home;