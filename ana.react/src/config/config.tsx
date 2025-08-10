

const isLocalhost = (): boolean => {
  const hostname = window.location.hostname;
  return hostname === 'localhost' || 
         hostname === '127.0.0.1' || 
         hostname === '::1';
};

const getApiBase = () => {
          const apiUrl=  "/api" //  import.meta.env.VITE_API_URL 
          return apiUrl;
        };

const getApiUrl = () => {
          if (isLocalhost()) {
            return "https://localhost:7001";
          }

          const hostname = window.location.hostname;
          if (hostname.includes('azurecontainerapps.io')) {
            return `https://${hostname.replace('ana-react', 'ana-api')}`;
          }
          
          return "https://anniversarynotification.com";
        };

const getPublicApiUrl = () => {
          if (isLocalhost()) {
            return "https://localhost:7001";
          }

          return "https://anniversarynotification.com";
        };

  const getPublicAppUrl = () => {
          if (isLocalhost()) {
            return "https://localhost:7001";
          }

          return "https://react.anniversarynotification.com";
        };

export const PUBLIC_URLS = {
  API_BASE: `${getApiBase()}`,
  API_URL: `${getApiUrl()}`,
  APP_BASE: `${getPublicAppUrl()}`,
  OIDC_AUTHORITY: (() => {
    return getPublicApiUrl();
  })(),

} as const;

export const APP_CONFIG = {
  OIDC_CLIENT_ID: "blazor",
  OIDC_SCOPES: 'openid profile email ana_api',
} as const;

export const AUTH_URLS = {
  LOGIN_REDIRECT: `${PUBLIC_URLS.API_URL}/account/login?returnUrl=${window.location.origin}`,
} as const;


export type PublicUrls = typeof PUBLIC_URLS;
export type AppConfig = typeof APP_CONFIG;
export type AuthUrls = typeof AUTH_URLS;


export default {
  PUBLIC_URLS,
  APP_CONFIG,
  AUTH_URLS,
} as const;