import type { NavigatorScreenParams } from '@react-navigation/native';

export type RootStackParamList = {
  Login: undefined;
  Main: NavigatorScreenParams<MainTabParamList>;
  StationDetail: { stationId: string };
  Session: undefined;
  QRScanner: undefined;
  Vehicles: undefined;
  Notifications: undefined;
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
