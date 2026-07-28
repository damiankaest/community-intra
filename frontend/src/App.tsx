import { useQuery } from '@tanstack/react-query'
import {
  Activity,
  Award,
  Boxes,
  Building2,
  CircleAlert,
  Factory,
  ListChecks,
  RefreshCw,
  Server,
  ShieldCheck,
  Users,
} from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { getHealth, getSystemInfo } from './api/system'

const modules = [
  {
    name: 'Organisationen',
    description: 'Eigene Intranets mit sauber getrennten Mandanten.',
    icon: Building2,
  },
  {
    name: 'Mitglieder',
    description: 'Einladungen, Abteilungen und angemessen wichtige Titel.',
    icon: Users,
  },
  {
    name: 'Projekte & Aufgaben',
    description: 'Gemeinsame Vorhaben – sogar mit dokumentiertem Ziel.',
    icon: ListChecks,
  },
  {
    name: 'Incidents',
    description: 'Betriebsstörungen melden, untersuchen und daraus lernen.',
    icon: CircleAlert,
  },
  {
    name: 'Auszeichnungen',
    description: 'Besondere Leistungen offiziell und halbseriös würdigen.',
    icon: Award,
  },
  {
    name: 'Konzernchronik',
    description: 'Strukturierte Aktivitäten statt unkontrollierter Gerüchte.',
    icon: Activity,
  },
]

function App() {
  const { t } = useTranslation()
  const systemInfo = useQuery({
    queryKey: ['system-info'],
    queryFn: getSystemInfo,
    refetchInterval: 30_000,
  })
  const health = useQuery({
    queryKey: ['health'],
    queryFn: getHealth,
    refetchInterval: 30_000,
  })

  const isChecking = systemInfo.isPending || health.isPending
  const isConnected = systemInfo.isSuccess && health.isSuccess
  const statusLabel = isChecking
    ? t('status.checking')
    : isConnected
      ? t('status.connected')
      : t('status.unavailable')

  const refresh = () => {
    void Promise.all([systemInfo.refetch(), health.refetch()])
  }

  return (
    <div className="min-h-screen overflow-hidden bg-[var(--theme-background)] text-[var(--theme-text)]">
      <div className="industrial-grid pointer-events-none fixed inset-0 opacity-50" />

      <header className="relative border-b border-white/10 bg-black/20 backdrop-blur">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-5 py-4 sm:px-8">
          <div className="flex items-center gap-3">
            <div className="flex size-10 items-center justify-center rounded-xl bg-[var(--theme-primary)] text-black shadow-[0_0_32px_rgba(245,158,11,0.2)]">
              <Factory aria-hidden="true" size={22} strokeWidth={2.2} />
            </div>
            <div>
              <p className="text-sm font-semibold tracking-wide text-white">
                Community Intranet
              </p>
              <p className="text-xs text-[var(--theme-muted)]">
                Systemgrundlage · Phase 2
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={refresh}
            className="group flex items-center gap-2 rounded-full border border-white/10 bg-white/5 px-3 py-2 text-xs text-[var(--theme-muted)] transition hover:border-white/20 hover:text-white focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--theme-primary)]"
            aria-label="Backendstatus erneut prüfen"
          >
            <span
              className={`size-2 rounded-full ${
                isChecking
                  ? 'animate-pulse bg-[var(--theme-warning)]'
                  : isConnected
                    ? 'bg-[var(--theme-success)]'
                    : 'bg-[var(--theme-danger)]'
              }`}
            />
            <span className="hidden sm:inline">{statusLabel}</span>
            <RefreshCw
              aria-hidden="true"
              className={`transition-transform group-hover:rotate-45 ${isChecking ? 'animate-spin' : ''}`}
              size={14}
            />
          </button>
        </div>
      </header>

      <main className="relative">
        <section className="mx-auto grid max-w-7xl gap-12 px-5 py-16 sm:px-8 sm:py-24 lg:grid-cols-[1.2fr_0.8fr] lg:items-center lg:py-32">
          <div>
            <div className="mb-6 inline-flex items-center gap-2 rounded-full border border-[var(--theme-primary)]/25 bg-[var(--theme-primary)]/10 px-3 py-1.5 text-xs font-semibold tracking-[0.16em] text-[var(--theme-primary)] uppercase">
              <ShieldCheck aria-hidden="true" size={14} />
              Mandantenfähig · modular · thematisierbar
            </div>

            <h1 className="max-w-4xl text-4xl leading-[1.05] font-black tracking-[-0.045em] text-white sm:text-6xl lg:text-7xl">
              Euer eigenes Intranet.
              <span className="block text-[var(--theme-primary)]">
                Erfreulich überorganisiert.
              </span>
            </h1>

            <p className="mt-7 max-w-2xl text-base leading-7 text-[var(--theme-muted)] sm:text-lg">
              Eine Plattform für Freundesgruppen, Gaming-Server, Vereine und
              kleine Communities. Seriöse Module treffen auf Theme Packs,
              kreative Jobtitel und genau die richtige Menge Konzernhumor.
            </p>

            <div className="mt-9 flex flex-wrap gap-3">
              <div className="flex items-center gap-3 rounded-xl border border-white/10 bg-white/5 px-4 py-3">
                <Server
                  aria-hidden="true"
                  className="text-[var(--theme-primary)]"
                  size={19}
                />
                <div>
                  <p className="text-xs text-[var(--theme-muted)]">Backend</p>
                  <p className="text-sm font-semibold text-white">
                    {statusLabel}
                  </p>
                </div>
              </div>
              <div className="flex items-center gap-3 rounded-xl border border-white/10 bg-white/5 px-4 py-3">
                <Boxes
                  aria-hidden="true"
                  className="text-[var(--theme-primary)]"
                  size={19}
                />
                <div>
                  <p className="text-xs text-[var(--theme-muted)]">Version</p>
                  <p className="text-sm font-semibold text-white">
                    {systemInfo.data?.version ?? '0.1.0'}
                  </p>
                </div>
              </div>
            </div>
          </div>

          <div className="relative">
            <div className="absolute -inset-8 rounded-full bg-[var(--theme-primary)]/10 blur-3xl" />
            <div className="relative overflow-hidden rounded-2xl border border-white/10 bg-[var(--theme-surface)] shadow-2xl shadow-black/30">
              <div className="flex items-center justify-between border-b border-white/10 px-5 py-4">
                <div>
                  <p className="text-xs font-bold tracking-[0.16em] text-[var(--theme-primary)] uppercase">
                    Systemlage
                  </p>
                  <p className="mt-1 text-lg font-semibold text-white">
                    {health.data?.status ??
                      (isChecking ? 'Checking' : 'Unknown')}
                  </p>
                </div>
                <div
                  className={`flex size-12 items-center justify-center rounded-full border ${
                    isConnected
                      ? 'border-emerald-400/20 bg-emerald-400/10 text-emerald-300'
                      : 'border-amber-400/20 bg-amber-400/10 text-amber-300'
                  }`}
                >
                  <Activity aria-hidden="true" size={24} />
                </div>
              </div>

              <div className="space-y-3 p-5">
                <StatusRow
                  label="API"
                  value={systemInfo.data?.status ?? statusLabel}
                  healthy={systemInfo.isSuccess}
                />
                <StatusRow
                  label="PostgreSQL"
                  value={health.data?.checks.postgresql?.status ?? statusLabel}
                  healthy={health.data?.checks.postgresql?.status === 'Healthy'}
                />
                <StatusRow
                  label="Umgebung"
                  value={systemInfo.data?.environment ?? 'Development'}
                  healthy
                />
              </div>

              <div className="border-t border-white/10 bg-black/15 px-5 py-4 text-xs leading-5 text-[var(--theme-muted)]">
                Alle Systeme werden beobachtet. Das Management bleibt aus
                Prinzip misstrauisch.
              </div>
            </div>
          </div>
        </section>

        <section className="border-y border-white/10 bg-black/15">
          <div className="mx-auto max-w-7xl px-5 py-16 sm:px-8 sm:py-20">
            <div className="max-w-2xl">
              <p className="text-xs font-bold tracking-[0.16em] text-[var(--theme-primary)] uppercase">
                In Vorbereitung
              </p>
              <h2 className="mt-3 text-3xl font-bold tracking-tight text-white sm:text-4xl">
                Module für produktive Unordnung
              </h2>
              <p className="mt-4 leading-7 text-[var(--theme-muted)]">
                Der technische Kern bleibt generisch. Begriffe, Farben, Texte
                und Vorschläge liefert später das gewählte Theme Pack.
              </p>
            </div>

            <div className="mt-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {modules.map(({ name, description, icon: Icon }) => (
                <article
                  key={name}
                  className="group rounded-2xl border border-white/10 bg-white/[0.035] p-5 transition hover:-translate-y-0.5 hover:border-[var(--theme-primary)]/30 hover:bg-white/[0.055]"
                >
                  <div className="flex size-10 items-center justify-center rounded-xl border border-white/10 bg-black/20 text-[var(--theme-primary)]">
                    <Icon aria-hidden="true" size={20} />
                  </div>
                  <h3 className="mt-5 font-semibold text-white">{name}</h3>
                  <p className="mt-2 text-sm leading-6 text-[var(--theme-muted)]">
                    {description}
                  </p>
                </article>
              ))}
            </div>
          </div>
        </section>
      </main>

      <footer className="relative mx-auto flex w-full max-w-7xl flex-col gap-2 px-5 py-8 text-xs text-[var(--theme-muted)] sm:flex-row sm:items-center sm:justify-between sm:px-8">
        <span>Community Intranet · Foundation 0.1.0</span>
        <span>Effizienz wird erwartet, aber noch nicht gemessen.</span>
      </footer>
    </div>
  )
}

interface StatusRowProps {
  label: string
  value: string
  healthy: boolean
}

function StatusRow({ label, value, healthy }: StatusRowProps) {
  return (
    <div className="flex items-center justify-between rounded-xl border border-white/[0.07] bg-black/15 px-4 py-3">
      <span className="text-sm text-[var(--theme-muted)]">{label}</span>
      <span className="flex items-center gap-2 text-sm font-medium text-white">
        <span
          className={`size-1.5 rounded-full ${
            healthy ? 'bg-[var(--theme-success)]' : 'bg-[var(--theme-warning)]'
          }`}
        />
        {value}
      </span>
    </div>
  )
}

export default App
