import type { NavigatorScreenParams } from '@react-navigation/native';

export type RootStackParamList = {
  Login: undefined;
  Register: undefined;
  OtpVerification: { phoneNumber: string };
  Main: NavigatorScreenParams<MainTabParamList>;
  StationDetail: { stationId: string };
  Session: undefined;
  QRScanner: undefined;
  Vehicles: undefined;
  Notifications: undefined;
  Settings: undefined;
  PaymentMethods: undefined;
  HelpSupport: undefined;
  Promotions: undefined;
};

export type MainTabParamList = {
  Home: undefined;
  Favorites: undefined;
  History: undefined;
  Wallet: undefined;
  Profile: undefined;
};

declare global {
  namespace ReactNavigation {
    interface RootParamList extends RootStackParamList {}
  }
}
