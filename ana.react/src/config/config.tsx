

const isLocalhost = (): boolean => {
  const hostname = window.location.hostname;
  return hostname === 'localhost' || 
         hostname === '127.0.0.1' || 
         hostname === '::1';
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
  OIDC_CLIENT_ID: "blazor",
  OIDC_SCOPES: 'openid profile email ana_api',
} as const;

export const AUTH_URLS = {
  LOGIN_REDIRECT: `${PUBLIC_URLS.API_BASE}/account/login?returnUrl=${window.location.origin}`,
} as const;


export type PublicUrls = typeof PUBLIC_URLS;
export type AppConfig = typeof APP_CONFIG;
export type AuthUrls = typeof AUTH_URLS;


export default {
  PUBLIC_URLS,
  APP_CONFIG,
  AUTH_URLS,
} as const;