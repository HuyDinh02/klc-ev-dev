import { Platform } from 'react-native';
import * as Notifications from 'expo-notifications';
import * as Device from 'expo-device';
import api from '../api/client';

// Configure how notifications appear when the app is in the foreground
Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: true,
  }),
});

let devicePushToken: string | null = null;

/**
 * Request push notification permissions and register the device token
 * with the backend. Call this after the user logs in.
 */
export async function registerForPushNotifications(): Promise<string | null> {
  // Push notifications only work on physical devices
  if (!Device.isDevice) {
    console.warn('Push notifications require a physical device');
    return null;
  }

  // Check and request permissions
  const { status: existingStatus } = await Notifications.getPermissionsAsync();
  let finalStatus = existingStatus;

  if (existingStatus !== 'granted') {
    const { status } = await Notifications.requestPermissionsAsync();
    finalStatus = status;
  }

  if (finalStatus !== 'granted') {
    console.warn('Push notification permission not granted');
    return null;
  }

  try {
    // Get the native device push token (FCM on Android, APNs on iOS)
    const tokenResponse = await Notifications.getDevicePushTokenAsync();
    const token = tokenResponse.data;
    devicePushToken = token;

    // Determine platform: 0 = iOS, 1 = Android (matches DevicePlatform enum)
    const platform = Platform.OS === 'ios' ? 'iOS' : 'Android';

    // Register with backend
    await api.post('/devices/register', {
      fcmToken: token,
      platform,
    });

    // Set up Android notification channel
    if (Platform.OS === 'android') {
      await Notifications.setNotificationChannelAsync('default', {
        name: 'Default',
        importance: Notifications.AndroidImportance.MAX,
        vibrationPattern: [0, 250, 250, 250],
        lightColor: '#2D9B3A',
      });
    }

    return token;
  } catch (error) {
    console.error('Failed to register for push notifications:', error);
    return null;
  }
}

/**
 * Unregister the device from push notifications.
 * Call this when the user logs out.
 */
export async function unregisterPushNotifications(): Promise<void> {
  try {
    if (devicePushToken) {
      await api.delete(`/devices/${encodeURIComponent(devicePushToken)}`);
      devicePushToken = null;
    }
  } catch (error) {
    // Best-effort: don't block logout if unregister fails
    console.error('Failed to unregister push notifications:', error);
  }
}

/**
 * Add a listener for notifications received while the app is in the foreground.
 * Returns a subscription that should be removed on cleanup.
 */
export function addNotificationReceivedListener(
  handler: (notification: Notifications.Notification) => void
): Notifications.Subscription {
  return Notifications.addNotificationReceivedListener(handler);
}

/**
 * Add a listener for when the user taps on a notification.
 * Returns a subscription that should be removed on cleanup.
 */
export function addNotificationResponseReceivedListener(
  handler: (response: Notifications.NotificationResponse) => void
): Notifications.Subscription {
  return Notifications.addNotificationResponseReceivedListener(handler);
}

/**
 * Get the last notification response (e.g., if the app was opened from a notification).
 */
export async function getLastNotificationResponse(): Promise<Notifications.NotificationResponse | null> {
  return Notifications.getLastNotificationResponseAsync();
}
