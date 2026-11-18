import { AppEnvironment } from './environment.model';

export const environment: AppEnvironment = {
  production: true,
  authConfig: {
    redirectUri: 'https://leohr.leobit.co/login',
    issuer: 'https://identity.leobit.co',
    postLogoutRedirectUri: 'https://leohr.leobit.co',
    showDebugInformation: false,
  },
  navigationData: 'prod',
};
