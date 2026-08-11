import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter } from 'react-router-dom'
import { registerSW } from 'virtual:pwa-register'
import App from './App'
import { FootballApp } from './components/FootballApp'
import { FootballLiveTrainingCompanion } from './components/FootballLiveTrainingCompanion'
import { FootballTrainingOperationsDock } from './components/FootballTrainingOperationsDock'
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
            <FootballTrainingOperationsDock />
            <FootballLiveTrainingCompanion />
          </>
        ) : <App />}
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
)
