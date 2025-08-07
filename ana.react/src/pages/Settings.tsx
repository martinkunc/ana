import React, { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { useSharedState } from '../contexts/SharedStateContext';
import { useApiClient } from '../hooks/useApiClient';
import { AnaUser } from '../services/ApiClient';

const Settings: React.FC = () => {
  const { user, isAuthenticated, logout } = useAuth();
  const { updateDisplayName } = useSharedState();
  const apiClient = useApiClient();
  
  const [settingsModel, setSettingsModel] = useState<AnaUser>({
    id: '',
    displayName: '',
    selectedGroupId: '',
    preferredNotification: 'None',
    whatsAppNumber: ''
  });
  
  const [saveStatusMessage, setSaveStatusMessage] = useState<string>('');
  const [isLoading, setIsLoading] = useState(false);
  
  useEffect(() => {
    const loadUserSettings = async () => {
      if (!isAuthenticated || !user) return;

      setIsLoading(true);
      try {
        const userId = user.profile?.sub;
        if (!userId) {
          throw new Error('User ID not found in claims');
        }

        const settings = await apiClient.getUserSettings(userId);
        console.log(`User settings retrieved: ${settings.displayName}, ${settings.whatsAppNumber}, ${settings.preferredNotification}`);
        setSettingsModel(settings);
      } catch (error) {
        console.error('Error loading user settings:', error);
        setSaveStatusMessage('Error loading settings');
      } finally {
        setIsLoading(false);
      }
    };

    loadUserSettings();
  }, [isAuthenticated, user, apiClient]);

  const handleInputChange = (field: keyof AnaUser, value: string) => {
    setSettingsModel(prev => ({
      ...prev,
      [field]: value
    }));
    // Clear status message when user starts typing
    if (saveStatusMessage) {
      setSaveStatusMessage('');
    }
  };

  const handleSaveSettings = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!user) return;

    try {
      await apiClient.updateUserSettings(settingsModel.id, settingsModel);
      updateDisplayName(settingsModel.displayName);
      setSaveStatusMessage('Settings saved successfully!');
      console.log(`Settings saved: ${settingsModel.displayName}, ${settingsModel.whatsAppNumber}, ${settingsModel.preferredNotification}`);
    } catch (error) {
      console.error('Error saving settings:', error);
      setSaveStatusMessage("Settings weren't saved!");
    }
  };

  const handleCancelAccount = async () => {
    const confirmed = window.confirm('Are you sure you want to cancel your account?');
    if (!confirmed) return;

    try {
      await apiClient.deleteUser(settingsModel.id);
      setSaveStatusMessage('User cancelled successfully!');
      console.log(`User cancelled: ${settingsModel.id}`);
      // Logout after successful account cancellation
      await logout();
    } catch (error) {
      console.error('Error cancelling account:', error);
      setSaveStatusMessage("User wasn't cancelled!");
    }
  };

  if (isLoading) {
    return (
      <div className="text-center">
        <div className="spinner-border" role="status">
          <span className="sr-only">Loading...</span>
        </div>
        <p>Loading settings...</p>
      </div>
    );
  }

  return (
    <div>
      <h1>Settings</h1>
      
      <form onSubmit={handleSaveSettings}>
        <div style={{ marginBottom: '1.5em' }}>
          <label>Display name</label><br />
          <input
            type="text"
            style={{
              width: '320px',
              fontSize: '1.2em',
              marginTop: '0.2em',
              marginBottom: '1em',
              padding: '0.3em'
            }}
            value={settingsModel.displayName}
            onChange={(e) => handleInputChange('displayName', e.target.value)}
          />
        </div>

        <div style={{ marginBottom: '1.5em' }}>
          <label>Phone for WhatsApp notification</label><br />
          <input
            type="text"
            style={{
              width: '320px',
              fontSize: '1.2em',
              marginTop: '0.2em',
              marginBottom: '1em',
              padding: '0.3em'
            }}
            value={settingsModel.whatsAppNumber}
            onChange={(e) => handleInputChange('whatsAppNumber', e.target.value)}
          />
        </div>

        <div style={{ marginBottom: '1.5em' }}>
          <label>Preferred notification</label><br />
          <div style={{
            display: 'flex',
            gap: '1.5em',
            marginTop: '0.5em',
            alignItems: 'center'
          }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: '0.5em' }}>
              <input
                type="radio"
                name="notification"
                value="Email"
                checked={settingsModel.preferredNotification === 'Email'}
                onChange={(e) => handleInputChange('preferredNotification', e.target.value)}
              />
              <span>Email</span>
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: '0.5em' }}>
              <input
                type="radio"
                name="notification"
                value="WhatsApp"
                checked={settingsModel.preferredNotification === 'WhatsApp'}
                onChange={(e) => handleInputChange('preferredNotification', e.target.value)}
              />
              <span>WhatsApp</span>
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: '0.5em' }}>
              <input
                type="radio"
                name="notification"
                value="None"
                checked={settingsModel.preferredNotification === 'None'}
                onChange={(e) => handleInputChange('preferredNotification', e.target.value)}
              />
              <span>None</span>
            </label>
          </div>
        </div>

        <div style={{ marginBottom: '1.5em', marginTop: '1.5em' }}>
          <button
            type="submit"
            style={{
              marginTop: '0.5em',
              border: '1px solid #fff',
              padding: '0.3em 1.5em',
              fontSize: '1.1em'
            }}
          >
            Save settings
          </button>
          
          {saveStatusMessage && (
            <div style={{
              color: saveStatusMessage.includes('successfully') ? '#4caf50' : '#f44336',
              marginTop: '1em'
            }}>
              {saveStatusMessage}
            </div>
          )}
        </div>
      </form>

      <div style={{ marginBottom: '1.5em', marginTop: '1.5em' }}>
        <label>Cancel my account</label><br />
        <button
          type="button"
          style={{
            marginTop: '0.5em',
            border: '1px solid #fff',
            padding: '0.3em 1.5em',
            fontSize: '1.1em'
          }}
          onClick={handleCancelAccount}
        >
          Cancel
        </button>
      </div>
    </div>
  );
};

export default Settings;
