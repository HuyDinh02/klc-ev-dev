import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import * as SecureStore from 'expo-secure-store';
import en from './en.json';
import vi from './vi.json';

const LANGUAGE_KEY = 'app_language';

i18n.use(initReactI18next).init({
  resources: {
    vi: { translation: vi },
    en: { translation: en },
  },
  lng: 'vi',
  fallbackLng: 'en',
  interpolation: {
    escapeValue: false,
  },
});

// Restore persisted language on startup
SecureStore.getItemAsync(LANGUAGE_KEY).then((lang) => {
  if (lang && (lang === 'vi' || lang === 'en')) {
    i18n.changeLanguage(lang);
  }
});

// Persist language changes
i18n.on('languageChanged', (lng) => {
  SecureStore.setItemAsync(LANGUAGE_KEY, lng).catch(() => {
    // Ignore storage errors — non-critical
  });
});

export default i18n;
