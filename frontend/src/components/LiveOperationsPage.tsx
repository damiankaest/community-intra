import { useState, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Activity,
  Clock3,
  Gauge,
  KeyRound,
  LockKeyhole,
  RefreshCw,
  Server,
  Settings2,
  ShieldCheck,
  Signal,
  Unplug,
  Users,
} from 'lucide-react'
import { useParams } from 'react-router-dom'
import type { CurrentUser } from '../api/auth'
import {
  disconnectGameServer,
  getGameServerConfiguration,
  getLiveServerStatus,
  saveGameServerConfiguration,
  testGameServerConnection,
  type LiveServerConnectionState,
  type SaveGameServerConfiguration,
} from '../api/liveOperations'
import { getOrganization } from '../api/organizations'
import { FeatureLayout } from './PhaseSixPages'

const statusLabels: Record<LiveServerConnectionState, string> = {
  NotConfigured: 'Noch nicht verbunden',
  Disabled: 'Anbindung pausiert',
  Online: 'Online',
  Reachable: 'Erreichbar',
  Offline: 'Nicht erreichbar',
  AuthenticationFailed: 'Token abgelehnt',
  UntrustedCertificate: 'Zertifikat bestätigen',
  CertificateChanged: 'Zertifikat geändert',
  ConfigurationError: 'Konfiguration prüfen',
}

const initialForm: SaveGameServerConfiguration = {
  displayName: 'Satisfactory Server',
  host: '',
  port: 7777,
  apiToken: '',
  certificateFingerprint: '',
  isEnabled: true,
}

export function LiveOperationsPage({ user }: { user: CurrentUser }) {
  const { organizationId = '' } = useParams()
  const queryClient = useQueryClient()
  const [showSettings, setShowSettings] = useState(false)
  const [formOverride, setForm] = useState<SaveGameServerConfiguration>()
  const organization = useQuery({
    queryKey: ['organization', organizationId],
    queryFn: () => getOrganization(organizationId),
    enabled: Boolean(organizationId),
  })
  const canManage =
    organization.data?.permissionRole === 'Owner' ||
    organization.data?.permissionRole === 'Administrator'
  const configuration = useQuery({
    queryKey: ['live-server-configuration', organizationId],
    queryFn: () => getGameServerConfiguration(organizationId),
    enabled: Boolean(organizationId && canManage),
  })
  const status = useQuery({
    queryKey: ['live-server-status', organizationId],
    queryFn: () => getLiveServerStatus(organizationId),
    enabled: Boolean(organizationId),
    refetchInterval: 30_000,
    retry: false,
  })

  const form =
    formOverride ??
    (configuration.data
      ? {
          displayName: configuration.data.displayName,
          host: configuration.data.host,
          port: configuration.data.port,
          apiToken: '',
          certificateFingerprint:
            configuration.data.certificateFingerprint ?? '',
          isEnabled: configuration.data.isEnabled,
          concurrencyToken: configuration.data.concurrencyToken,
        }
      : initialForm)

  const testMutation = useMutation({
    mutationFn: () =>
      testGameServerConnection(organizationId, {
        displayName: form.displayName,
        host: form.host,
        port: Number(form.port),
        apiToken: form.apiToken || undefined,
        certificateFingerprint: form.certificateFingerprint || undefined,
      }),
    onSuccess: (result) => {
      queryClient.setQueryData(['live-server-status', organizationId], result)
    },
  })
  const saveMutation = useMutation({
    mutationFn: () =>
      saveGameServerConfiguration(organizationId, {
        ...form,
        port: Number(form.port),
        apiToken: form.apiToken || undefined,
        certificateFingerprint: form.certificateFingerprint || undefined,
      }),
    onSuccess: async (result) => {
      setForm({
        ...form,
        apiToken: '',
        concurrencyToken: result.concurrencyToken,
      })
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ['live-server-configuration', organizationId],
        }),
        queryClient.invalidateQueries({
          queryKey: ['live-server-status', organizationId],
        }),
        queryClient.invalidateQueries({
          queryKey: ['activities', organizationId],
        }),
      ])
      setShowSettings(false)
    },
  })
  const disconnectMutation = useMutation({
    mutationFn: () => disconnectGameServer(organizationId),
    onSuccess: async () => {
      setForm({ ...initialForm })
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ['live-server-configuration', organizationId],
        }),
        queryClient.invalidateQueries({
          queryKey: ['live-server-status', organizationId],
        }),
      ])
    },
  })

  const currentStatus = status.data
  const certificateStatus = testMutation.data ?? status.data
  const pendingFingerprint =
    certificateStatus?.state === 'UntrustedCertificate' ||
    certificateStatus?.state === 'CertificateChanged'
      ? certificateStatus.presentedCertificateFingerprint
      : undefined
  const error =
    testMutation.error ?? saveMutation.error ?? disconnectMutation.error

  return (
    <FeatureLayout
      user={user}
      title="Gameserver"
      subtitle="Live-Zustand eures Satisfactory-Servers – lesbar für alle, konfigurierbar nur durch Owner und Admins."
    >
      <section className="overflow-hidden rounded-3xl border border-white/10 bg-[var(--theme-surface)] shadow-2xl shadow-black/20">
        <div className="relative overflow-hidden p-6 sm:p-8">
          <div className="absolute inset-0 bg-gradient-to-br from-[var(--theme-primary)]/15 via-transparent to-transparent" />
          <div className="relative flex flex-wrap items-start justify-between gap-5">
            <div className="flex items-start gap-4">
              <div
                className={`rounded-2xl p-3 ${currentStatus?.state === 'Online' ? 'bg-[var(--theme-success)]/15 text-[var(--theme-success)]' : 'bg-white/10 text-[var(--theme-muted)]'}`}
              >
                <Server size={28} />
              </div>
              <div>
                <div className="flex flex-wrap items-center gap-2">
                  <h2 className="text-2xl font-black text-white">
                    {currentStatus?.displayName ?? 'Satisfactory Server'}
                  </h2>
                  <StatusBadge state={currentStatus?.state} />
                </div>
                <p className="mt-2 max-w-2xl text-sm leading-6 text-[var(--theme-muted)]">
                  {status.isPending
                    ? 'Serverstatus wird geladen …'
                    : (currentStatus?.message ??
                      status.error?.message ??
                      'Der Status konnte nicht geladen werden.')}
                </p>
                {currentStatus?.host && (
                  <p className="mt-2 font-mono text-xs text-[var(--theme-text)]">
                    {currentStatus.host}:{currentStatus.port}
                  </p>
                )}
              </div>
            </div>
            <div className="flex gap-2">
              <button
                type="button"
                className="feature-button inline-flex items-center gap-2"
                disabled={status.isFetching}
                onClick={() =>
                  void queryClient.fetchQuery({
                    queryKey: ['live-server-status', organizationId],
                    queryFn: () => getLiveServerStatus(organizationId, true),
                  })
                }
              >
                <RefreshCw
                  size={16}
                  className={status.isFetching ? 'animate-spin' : ''}
                />
                Neu laden
              </button>
              {canManage && (
                <button
                  type="button"
                  className="rounded-xl border border-white/10 bg-white/[0.04] p-3 text-white hover:border-[var(--theme-primary)]/50"
                  aria-label="Servereinstellungen"
                  onClick={() => setShowSettings((current) => !current)}
                >
                  <Settings2 size={18} />
                </button>
              )}
            </div>
          </div>
        </div>

        <div className="grid border-t border-white/10 sm:grid-cols-2 xl:grid-cols-4">
          <ServerMetric
            icon={<Users size={18} />}
            label="Spieler"
            value={
              currentStatus?.connectedPlayers == null
                ? '–'
                : `${currentStatus.connectedPlayers} / ${currentStatus.playerLimit ?? '–'}`
            }
          />
          <ServerMetric
            icon={<Signal size={18} />}
            label="Session"
            value={currentStatus?.activeSessionName ?? '–'}
          />
          <ServerMetric
            icon={<Gauge size={18} />}
            label="Fortschritt"
            value={
              [
                currentStatus?.gamePhase,
                currentStatus?.techTier == null
                  ? undefined
                  : `Tier ${currentStatus.techTier}`,
              ]
                .filter(Boolean)
                .join(' · ') || '–'
            }
          />
          <ServerMetric
            icon={<Activity size={18} />}
            label="Tickrate"
            value={
              currentStatus?.averageTickRate == null
                ? '–'
                : `${currentStatus.averageTickRate.toFixed(1)} TPS`
            }
          />
        </div>
      </section>

      {currentStatus?.state === 'Online' && (
        <div className="mt-6 grid gap-4 sm:grid-cols-2">
          <InfoCard
            icon={<Clock3 size={19} />}
            label="Laufzeit"
            value={formatDuration(currentStatus.totalGameDurationSeconds)}
            detail={
              currentStatus.isGamePaused
                ? 'Spiel ist gerade pausiert'
                : currentStatus.isGameRunning
                  ? 'Spielstand läuft'
                  : 'Kein Spielstand geladen'
            }
          />
          <InfoCard
            icon={<ShieldCheck size={19} />}
            label="Aktiver Meilenstein"
            value={currentStatus.activeSchematic ?? 'Keiner ausgewählt'}
            detail={`API: ${currentStatus.health ?? 'unbekannt'}`}
          />
        </div>
      )}

      {showSettings && canManage && (
        <section className="mt-6 rounded-3xl border border-white/10 bg-[var(--theme-surface)] p-6">
          <div className="flex items-start gap-3">
            <LockKeyhole
              className="mt-1 text-[var(--theme-primary)]"
              size={20}
            />
            <div>
              <h2 className="text-xl font-black text-white">
                Satisfactory verbinden
              </h2>
              <p className="mt-1 text-sm leading-6 text-[var(--theme-muted)]">
                Host ohne <code>https://</code>. Das API-Token bleibt
                verschlüsselt im Backend und wird nie an den Browser
                zurückgegeben.
              </p>
            </div>
          </div>

          <form
            className="feature-form mt-6"
            onSubmit={(event) => {
              event.preventDefault()
              saveMutation.mutate()
            }}
          >
            <label>
              <span>Anzeigename</span>
              <input
                required
                maxLength={120}
                value={form.displayName}
                onChange={(event) =>
                  setForm({ ...form, displayName: event.target.value })
                }
              />
            </label>
            <label>
              <span>Host oder öffentliche IP</span>
              <input
                required
                placeholder="factory.example.net"
                value={form.host}
                onChange={(event) =>
                  setForm({ ...form, host: event.target.value })
                }
              />
            </label>
            <label>
              <span>HTTPS-/Spielport</span>
              <input
                required
                type="number"
                min={1}
                max={65535}
                value={form.port}
                onChange={(event) =>
                  setForm({ ...form, port: Number(event.target.value) })
                }
              />
            </label>
            <label>
              <span>
                API-Token{' '}
                {configuration.data?.hasApiToken
                  ? '(leer lassen = behalten)'
                  : ''}
              </span>
              <span className="relative">
                <KeyRound
                  size={16}
                  className="pointer-events-none absolute top-3 left-3 text-[var(--theme-muted)]"
                />
                <input
                  type="password"
                  autoComplete="off"
                  required={!configuration.data?.hasApiToken}
                  className="pl-10"
                  placeholder="server.GenerateAPIToken"
                  value={form.apiToken}
                  onChange={(event) =>
                    setForm({ ...form, apiToken: event.target.value })
                  }
                />
              </span>
            </label>
            <label className="sm:col-span-2">
              <span>Bestätigter SHA-256-Zertifikat-Fingerprint</span>
              <input
                className="font-mono text-xs"
                placeholder="Wird beim ersten Test angezeigt"
                value={form.certificateFingerprint}
                onChange={(event) =>
                  setForm({
                    ...form,
                    certificateFingerprint: event.target.value,
                  })
                }
              />
            </label>
            <label className="flex-row items-center gap-3">
              <input
                type="checkbox"
                checked={form.isEnabled}
                onChange={(event) =>
                  setForm({ ...form, isEnabled: event.target.checked })
                }
              />
              <span>Anbindung aktiv</span>
            </label>
            <div className="flex flex-wrap gap-2 sm:col-span-2">
              <button
                type="button"
                className="rounded-xl border border-white/10 bg-white/[0.04] px-4 py-3 text-sm font-bold text-white hover:border-[var(--theme-primary)]/50"
                disabled={testMutation.isPending}
                onClick={() => testMutation.mutate()}
              >
                {testMutation.isPending
                  ? 'Teste Verbindung …'
                  : 'Verbindung testen'}
              </button>
              <button
                type="submit"
                className="feature-button"
                disabled={saveMutation.isPending}
              >
                {saveMutation.isPending ? 'Speichert …' : 'Sicher speichern'}
              </button>
            </div>
          </form>

          {testMutation.data && (
            <div className="mt-4 rounded-2xl border border-white/10 bg-black/20 p-4 text-sm text-[var(--theme-muted)]">
              <strong className="text-white">
                {statusLabels[testMutation.data.state]}
              </strong>
              <p className="mt-1">{testMutation.data.message}</p>
              {pendingFingerprint && (
                <button
                  type="button"
                  className="mt-3 rounded-xl bg-[var(--theme-primary)] px-4 py-2 font-bold text-black"
                  onClick={() =>
                    setForm({
                      ...form,
                      certificateFingerprint: pendingFingerprint,
                    })
                  }
                >
                  Diesen Fingerprint übernehmen
                </button>
              )}
            </div>
          )}

          {error && (
            <p className="mt-4 rounded-xl border border-[var(--theme-danger)]/30 bg-[var(--theme-danger)]/10 p-3 text-sm text-[var(--theme-danger)]">
              {error.message}
            </p>
          )}

          {configuration.data && (
            <div className="mt-6 border-t border-white/10 pt-5">
              <button
                type="button"
                className="inline-flex items-center gap-2 text-sm font-bold text-[var(--theme-danger)]"
                disabled={disconnectMutation.isPending}
                onClick={() => {
                  if (
                    window.confirm(
                      'Gameserver wirklich vom Intranet trennen? Der Gameserver selbst bleibt unverändert.',
                    )
                  ) {
                    disconnectMutation.mutate()
                  }
                }}
              >
                <Unplug size={16} />
                Verbindung entfernen
              </button>
            </div>
          )}
        </section>
      )}

      {!canManage && (
        <p className="mt-6 flex items-center gap-2 text-sm text-[var(--theme-muted)]">
          <LockKeyhole size={15} />
          Nur Owner und Admins können die Serververbindung ändern.
        </p>
      )}
    </FeatureLayout>
  )
}

function StatusBadge({ state }: { state?: LiveServerConnectionState }) {
  return (
    <span
      className={`rounded-full px-2.5 py-1 text-xs font-black ${state === 'Online' ? 'bg-[var(--theme-success)]/15 text-[var(--theme-success)]' : state === 'UntrustedCertificate' || state === 'CertificateChanged' ? 'bg-[var(--theme-warning)]/15 text-[var(--theme-warning)]' : 'bg-white/10 text-[var(--theme-muted)]'}`}
    >
      {state ? statusLabels[state] : 'Wird geprüft'}
    </span>
  )
}

function ServerMetric({
  icon,
  label,
  value,
}: {
  icon: ReactNode
  label: string
  value: string
}) {
  return (
    <div className="border-white/10 p-5 not-first:border-t sm:not-first:border-t-0 sm:not-first:border-l">
      <div className="flex items-center gap-2 text-xs font-bold tracking-wider text-[var(--theme-muted)] uppercase">
        {icon}
        {label}
      </div>
      <p className="mt-2 truncate text-lg font-black text-white">{value}</p>
    </div>
  )
}

function InfoCard({
  icon,
  label,
  value,
  detail,
}: {
  icon: ReactNode
  label: string
  value: string
  detail: string
}) {
  return (
    <div className="rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5">
      <div className="flex items-center gap-2 text-[var(--theme-primary)]">
        {icon}
        <span className="text-xs font-black tracking-wider uppercase">
          {label}
        </span>
      </div>
      <p className="mt-3 text-lg font-black text-white">{value}</p>
      <p className="mt-1 text-sm text-[var(--theme-muted)]">{detail}</p>
    </div>
  )
}

function formatDuration(seconds?: number) {
  if (seconds == null) return '–'
  const days = Math.floor(seconds / 86_400)
  const hours = Math.floor((seconds % 86_400) / 3_600)
  return days > 0 ? `${days} T ${hours} Std` : `${hours} Std`
}
