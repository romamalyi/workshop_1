export interface UserProfile {
  email: string;
  name: string;
  picture: string;
  sub: string;
  LeoHRManage?: ClaimValue;
  LeoHROnboarding?: ClaimValue;
}

// Permission levels from the Time Tracking Permission Matrix, as the server's ClaimValue.
export enum ClaimValue {
  Read = 1,
  Write = 2,
  ReadWrite = 3,
}
