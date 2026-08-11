import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { registerSW } from 'virtual:pwa-register'
import App from './App'
import { FootballApp } from './components/FootballApp'
import { FootballFeedbackDock } from './components/FootballFeedbackDock'
import { FootballReadinessDock } from './components/FootballReadinessDock'
import { FootballTrainerReadinessPanel } from './components/FootballTrainerReadinessPanel'
import './i18n'
import './index.css'
import './football.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 15_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})

registerSW({ immediate: true })

const isFootballRoute = window.location.pathname === '/football'
  || window.location.pathname.startsWith('/football/')

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        {isFootballRoute ? (
          <>
            <FootballApp />
            <FootballReadinessDock />
            <FootballFeedbackDock />
            <FootballTrainerReadinessPanel />
          </>
        ) : <App />}
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
)
