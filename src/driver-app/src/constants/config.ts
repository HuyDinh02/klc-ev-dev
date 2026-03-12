export const Config = {
  // Driver BFF API
  API_BASE_URL: __DEV__
    ? 'http://localhost:5001/api/v1'
    : 'https://bff.ev.odcall.com/api/v1',

  // SignalR Hub
  SIGNALR_HUB_URL: __DEV__
    ? 'http://localhost:5001/hubs/driver'
    : 'https://bff.ev.odcall.com/hubs/driver',

  // Google Maps (placeholder - need real key)
  GOOGLE_MAPS_API_KEY: 'YOUR_GOOGLE_MAPS_API_KEY',

  // Default map region (Vietnam)
  DEFAULT_REGION: {
    latitude: 21.0285,  // Hanoi
    longitude: 105.8542,
    latitudeDelta: 0.1,
    longitudeDelta: 0.1,
  },

  // Refresh intervals
  SESSION_REFRESH_INTERVAL: 10000, // 10 seconds
  STATION_CACHE_TTL: 60000,        // 1 minute
};
