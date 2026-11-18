// This file can be replaced during build by using the `fileReplacements` array.
// `ng build` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

import { AppEnvironment } from './environment.model';

export const environment: AppEnvironment = {
  production: false,
  authConfig: {
    redirectUri: 'https://localhost:5002/login',
    issuer: 'https://test.identity.leobit.co',
    postLogoutRedirectUri: 'https://localhost:5002',
    showDebugInformation: true,
  },
  navigationData: 'test',
};
