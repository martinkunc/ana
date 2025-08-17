import React from 'react';
import { AuthContextType } from './AuthContext';


// Helper builds an unsigned dummy JWT (alg=none) for local bypass.
function buildDummyToken(): string {
  const header = btoa(JSON.stringify({ alg: 'none', typ: 'JWT' }));
  const now = Math.floor(Date.now() / 1000);
  const payload = btoa(JSON.stringify({
    iss: 'local-bypass',
    aud: 'ana_api',
    sub: 'local-bypass',
    name: 'Local Bypass User',
    preferred_username: 'local-bypass',
    role: ['Tester'],
    iat: now,
    exp: now + 3600
  }));
  return `${header}.${payload}.`;
}

export const FakeAuthProviderInner: React.FC<{ children: React.ReactNode, ContextOverride: React.Context<AuthContextType | undefined> }> = ({ children, ContextOverride }) => {
  const fakeUser: any = {
    profile: {
      sub: 'local-bypass',
      name: 'Local Bypass User',
      preferred_username: 'local-bypass',
      role: ['Tester'],
    },
    access_token: buildDummyToken(),
    id_token: 'dummy',
    expired: false,
  };
  const value: AuthContextType = {
    user: fakeUser,
    isAuthenticated: true,
    isLoading: false,
    isLoggingOut: false,
    userManager: null,
    login: async () => {},
    logout: async () => {},
    getAccessToken: () => fakeUser.access_token,
    getIdToken: () => fakeUser.id_token,
    getUserClaims: () => fakeUser.profile,
  };
  return <ContextOverride.Provider value={value}>{children}</ContextOverride.Provider>;
};
