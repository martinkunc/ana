import React from 'react';
import { DISABLE_AUTH } from '../config/config';
import { AuthProvider, AuthContext } from './AuthContext';
import { FakeAuthProviderInner } from './FakeAuthProvider';

export const AppAuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  if (DISABLE_AUTH) {
    console.warn('[Auth] DISABLE_AUTH=true - using fake authentication');
    return <FakeAuthProviderInner ContextOverride={AuthContext}>{children}</FakeAuthProviderInner>;
  }
  return <AuthProvider>{children}</AuthProvider>;
};
