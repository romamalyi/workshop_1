export type NavigationEnv = 'test' | 'prod';

export interface AppEnvironment {
  production: boolean;
  authConfig: {
    redirectUri: string;
    issuer: string;
    postLogoutRedirectUri: string;
    showDebugInformation: boolean;
  };
  navigationData: NavigationEnv;
}
