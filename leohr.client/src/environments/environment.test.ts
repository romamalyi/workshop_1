import { AppEnvironment } from './environment.model';

export const environment: AppEnvironment = {
  production: false,
  authConfig: {
    redirectUri: 'https://test.leohr.leobit.co/login',
    issuer: 'https://test.identity.leobit.co',
    postLogoutRedirectUri: 'https://test.leohr.leobit.co',
    showDebugInformation: true,
  },
  navigationData: 'test',
};
