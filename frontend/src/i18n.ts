import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'

void i18n.use(initReactI18next).init({
  lng: 'de',
  fallbackLng: 'de',
  interpolation: {
    escapeValue: false,
  },
  resources: {
    de: {
      translation: {
        status: {
          connected: 'Backend verbunden',
          unavailable: 'Backend nicht erreichbar',
          checking: 'Verbindung wird geprüft',
        },
      },
    },
  },
})

export default i18n
