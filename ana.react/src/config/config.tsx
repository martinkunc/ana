

const getHostname = (): string => window.location.hostname;
const getProtocol = (): string => window.location.protocol;
const getPort = (): string => window.location.port;

const isLocalhost = (): boolean => {
  const hostname = getHostname();
  return hostname === 'localhost' || 
         hostname === '127.0.0.1' || 
         hostname === '::1';
};

const isDevelopment = (): boolean => {
  return process.env.NODE_ENV === 'development' || isLocalhost();
};

const isProduction = (): boolean => {
  return process.env.NODE_ENV === 'production' && !isLocalhost();
};

const getApiHost = () => {
          
          if (isLocalhost()) {
            return "https://localhost:7001";
          }

          // Production/deployed environment fallback
          const hostname = window.location.hostname;
          if (hostname.includes('azurecontainerapps.io')) {
            return `https://${hostname.replace('ana-react', 'ana-api')}`;
          }
          
          // Default fallback
          return "https://anniversarynotification.com";
        };

export const PUBLIC_URLS = {
  API_BASE: `${getApiHost()}`,

  OIDC_AUTHORITY: (() => {
    return getApiHost();
  })(),

} as const;

export const APP_CONFIG = {
  APP_NAME: 'Anniversary Notification Application',

  OIDC_CLIENT_ID: "blazor",
  
  OIDC_SCOPES: 'openid profile email ana_api',
  
} as const;

export const AUTH_URLS = {
  LOGIN_CALLBACK: `${window.location.origin}/authentication/login-callback`,
  LOGOUT_CALLBACK: `${window.location.origin}/authentication/logout-callback`,
  SILENT_CALLBACK: `${window.location.origin}/authentication/silent-callback`,
  LOGIN_REDIRECT: `${window.location.origin}/authentication/login`,
  LOGOUT_REDIRECT: `${window.location.origin}/authentication/logout`,
} as const;

export const API_ENDPOINTS = {

  USERS: {
    CREATE: `${PUBLIC_URLS.API_BASE}/api/v1/user`,
    GROUPS: (userId: string) => `${PUBLIC_URLS.API_BASE}/api/v1/user/groups/${userId}`,
    SELECTED_GROUP: (userId: string) => `${PUBLIC_URLS.API_BASE}/api/v1/user/select-group/${userId}`,
    SELECT_GROUP: (userId: string, groupId: string) => `${PUBLIC_URLS.API_BASE}/api/v1/user/select-group/${userId}/${groupId}`,
    SETTINGS: (userId: string) => `${PUBLIC_URLS.API_BASE}/api/v1/user/${userId}`,
    DELETE: (userId: string) => `${PUBLIC_URLS.API_BASE}/api/v1/user/${userId}`,
  },
  
  GROUPS: {
    CREATE: `${PUBLIC_URLS.API_BASE}/api/v1/group`,
    MEMBERS: (groupId: string) => `${PUBLIC_URLS.API_BASE}/api/v1/group/${groupId}/members`,
    MEMBER: (groupId: string) => `${PUBLIC_URLS.API_BASE}/api/v1/group/${groupId}/member`,
    MEMBER_ROLE: (groupId: string, userId: string) => `${PUBLIC_URLS.API_BASE}/api/v1/group/${groupId}/member/${userId}/role`,
    DELETE_MEMBER: (groupId: string, userId: string) => `${PUBLIC_URLS.API_BASE}/api/v1/group/${groupId}/member/${userId}`,
    ANNIVERSARIES: (groupId: string) => `${PUBLIC_URLS.API_BASE}/api/v1/group/${groupId}/anniversaries`,
    ANNIVERSARY: (groupId: string) => `${PUBLIC_URLS.API_BASE}/api/v1/group/${groupId}/anniversary`,
    ANNIVERSARY_DETAIL: (groupId: string, anniversaryId: string) => `${PUBLIC_URLS.API_BASE}/api/v1/group/${groupId}/anniversary/${anniversaryId}`,
  },
  
  SYSTEM: {
    DAILY_TASK: `${PUBLIC_URLS.API_BASE}/api/v1/daily-task`,
  },
  
  AUTH: {
    LOGIN: `${PUBLIC_URLS.API_BASE}/auth/login`,
    LOGOUT: `${PUBLIC_URLS.API_BASE}/auth/logout`,
    REFRESH: `${PUBLIC_URLS.API_BASE}/auth/refresh`,
    PROFILE: `${PUBLIC_URLS.API_BASE}/auth/profile`,
  },
} as const;

export type PublicUrls = typeof PUBLIC_URLS;
export type AppConfig = typeof APP_CONFIG;
export type AuthUrls = typeof AUTH_URLS;
export type ApiEndpoints = typeof API_ENDPOINTS;


export const ENVIRONMENT_INFO = {
  NODE_ENV: process.env.NODE_ENV,
  IS_DEVELOPMENT: isDevelopment(),
  IS_PRODUCTION: isProduction(),
  IS_LOCALHOST: isLocalhost(),
  HOSTNAME: getHostname(),
  PROTOCOL: getProtocol(),
  PORT: getPort(),
} as const;


export default {
  PUBLIC_URLS,
  APP_CONFIG,
  AUTH_URLS,
  API_ENDPOINTS,
  ENVIRONMENT_INFO,
} as const;