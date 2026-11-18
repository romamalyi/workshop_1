import { environment } from 'src/environments/environment';

export const authConfig = {
  responseType: 'code',
  redirectUri: environment.authConfig.redirectUri,
  issuer: environment.authConfig.issuer,
  postLogoutRedirectUri: environment.authConfig.postLogoutRedirectUri,
  clientId: 'leobit.leohr.web',
  showDebugInformation: environment.authConfig.showDebugInformation,
  scope: 'openid profile leohr offline_access',
  skipIssuerCheck: true,
  strictDiscoveryDocumentValidation: false,
};
