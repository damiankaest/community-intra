import { useState, type ReactNode } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  ArrowLeft,
  Check,
  Link2,
  LockKeyhole,
  Mail,
  ShieldCheck,
  Unlink,
} from 'lucide-react'
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom'
import {
  createConnectedAccountLink,
  disconnectAccount,
  forgotPassword,
  getAuthProviders,
  getConnectedAccounts,
  resetPassword,
  type CurrentUser,
} from '../api/auth'
import { ApiError } from '../api/client'

export function ExternalLoginButtons({ returnUrl = '/organizations' }: { returnUrl?: string }) {
  const providers = useQuery({ queryKey: ['auth-providers'], queryFn: getAuthProviders })
  return (
    <div className="external-login-stack">
      {providers.data?.discord && (
        <a className="external-login discord" href={`/api/auth/external/Discord/start?returnUrl=${encodeURIComponent(returnUrl)}`}>
          <span>◖◗</span> Mit Discord anmelden
        </a>
      )}
      {providers.data?.google && (
        <a className="external-login google" href={`/api/auth/external/Google/start?returnUrl=${encodeURIComponent(returnUrl)}`}>
          <span>G</span> Mit Google anmelden
        </a>
      )}
      {(providers.data?.discord || providers.data?.google) && <div className="external-divider"><span>oder mit E-Mail</span></div>}
    </div>
  )
}

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const mutation = useMutation({ mutationFn: () => forgotPassword(email) })
  return (
    <AuthSurface>
      <Mail />
      <span>ACCOUNT RECOVERY</span>
      <h1>Passwort vergessen?</h1>
      <p>Gib deine E-Mail-Adresse ein. Die Antwort bleibt aus Sicherheitsgründen immer gleich.</p>
      {mutation.data ? (
        <div className="auth-success"><Check /><p>{mutation.data.message}</p></div>
      ) : (
        <form onSubmit={(event) => { event.preventDefault(); mutation.mutate() }}>
          <label>E-Mail-Adresse<input type="email" required value={email} onChange={(event) => setEmail(event.target.value)} autoComplete="email" /></label>
          {mutation.error && <InlineAuthError error={mutation.error} />}
          <button disabled={mutation.isPending}>{mutation.isPending ? 'Wird gesendet …' : 'Reset-Link anfordern'}</button>
        </form>
      )}
      <Link to="/login"><ArrowLeft /> Zurück zur Anmeldung</Link>
    </AuthSurface>
  )
}

export function ResetPasswordPage() {
  const [params] = useSearchParams()
  const navigate = useNavigate()
  const email = params.get('email') ?? ''
  const token = params.get('token') ?? ''
  const [password, setPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const mutation = useMutation({
    mutationFn: () => resetPassword(email, token, password),
    onSuccess: () => setTimeout(() => navigate('/login?reset=success', { replace: true }), 900),
  })
  if (!email || !token) return <Navigate to="/forgot-password" replace />
  const mismatch = confirmation.length > 0 && password !== confirmation
  return (
    <AuthSurface>
      <LockKeyhole />
      <span>NEW CREDENTIALS</span>
      <h1>Neues Passwort</h1>
      <p>Der Link ist eine Stunde gültig und nach erfolgreichem Reset nicht erneut nutzbar.</p>
      <form onSubmit={(event) => { event.preventDefault(); if (!mismatch) mutation.mutate() }}>
        <label>Neues Passwort<input type="password" minLength={12} required value={password} onChange={(event) => setPassword(event.target.value)} autoComplete="new-password" /></label>
        <label>Passwort bestätigen<input type="password" minLength={12} required value={confirmation} onChange={(event) => setConfirmation(event.target.value)} autoComplete="new-password" /></label>
        {mismatch && <div className="auth-inline-error">Die Passwörter stimmen nicht überein.</div>}
        {mutation.error && <InlineAuthError error={mutation.error} />}
        {mutation.isSuccess && <div className="auth-success"><Check /> Passwort geändert. Du wirst weitergeleitet.</div>}
        <button disabled={mutation.isPending || mismatch}>{mutation.isPending ? 'Wird gespeichert …' : 'Passwort ändern'}</button>
      </form>
    </AuthSurface>
  )
}

export function AccountPage({ user }: { user: CurrentUser }) {
  const queryClient = useQueryClient()
  const [params] = useSearchParams()
  const connectionStatus = params.get('auth')
  const accounts = useQuery({ queryKey: ['connected-accounts'], queryFn: getConnectedAccounts })
  const connect = useMutation({
    mutationFn: (provider: 'google' | 'discord' | 'steam') => createConnectedAccountLink(provider),
    onSuccess: ({ url }) => { window.location.href = url },
  })
  const disconnect = useMutation({
    mutationFn: (provider: 'google' | 'discord' | 'steam') => disconnectAccount(provider),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['connected-accounts'] }),
  })
  return (
    <div className="account-page">
      <header><Link to="/organizations"><ArrowLeft /> CouchClash</Link><span>ACCOUNT SETTINGS</span><h1>{user.displayName}</h1><p>{user.email}</p></header>
      <section>
        <div className="account-section-head"><ShieldCheck /><div><h2>Connected Accounts</h2><p>Login-Anbieter und Steam-Identität zentral verwalten.</p></div></div>
        {connectionStatus && <div className={connectionStatus === 'linked' || connectionStatus === 'steam_linked' ? 'auth-success' : 'auth-inline-error'}>{connectedAccountMessage(connectionStatus)}</div>}
        {accounts.isPending && <p>Verbindungen werden geladen …</p>}
        {accounts.error && <InlineAuthError error={accounts.error} />}
        <div className="connected-account-list">
          {accounts.data && ([
            ['google', 'Google', 'G', accounts.data.google],
            ['discord', 'Discord', '◖◗', accounts.data.discord],
            ['steam', 'Steam', '●', accounts.data.steam],
          ] as const).map(([key, label, icon, account]) => (
            <div key={key}>
              <span className={`provider-icon ${key}`}>{icon}</span>
              <div><b>{label}</b><small>{account.connected ? account.displayName ?? (key === 'steam' ? account.steamId64 : 'Verbunden') : key === 'steam' ? 'Ordnet dich hochgeladenen Demos zu – kein Match-Sync' : 'Alternative Anmeldung'}</small></div>
              {account.connected ? <button className="disconnect" onClick={() => disconnect.mutate(key)} disabled={disconnect.isPending}><Unlink /> Trennen</button> : <button onClick={() => connect.mutate(key)} disabled={connect.isPending}><Link2 /> Verbinden</button>}
            </div>
          ))}
        </div>
        {connect.error && <InlineAuthError error={connect.error} />}
        {disconnect.error && <InlineAuthError error={disconnect.error} />}
      </section>
      <section className="account-cs2-callout"><div><span>COUCHCLASH CS2</span><h2>Steam erkennt dich in importierten Demos.</h2><p>Letzte Matches werden noch nicht automatisch geladen. Lade derzeit eine .dem-Datei hoch, damit CouchClash deine ausführlichen Stats auswertet.</p></div><Link to="/cs2">CS2 öffnen →</Link></section>
    </div>
  )
}

function connectedAccountMessage(status: string) {
  switch (status) {
    case 'linked': return 'Der Login-Anbieter wurde verbunden.'
    case 'steam_linked': return 'Steam wurde verbunden. Neu hochgeladene Demos können dich jetzt automatisch zuordnen.'
    case 'link_conflict':
    case 'steam_conflict': return 'Dieser externe Account gehört bereits zu einem anderen Konto.'
    case 'steam_link_expired':
    case 'link_expired': return 'Der Verknüpfungsvorgang ist abgelaufen. Bitte starte ihn erneut.'
    default: return 'Die Verbindung konnte nicht abgeschlossen werden.'
  }
}

function AuthSurface({ children }: { children: ReactNode }) {
  return <main className="auth-surface"><section>{children}</section></main>
}

function InlineAuthError({ error }: { error: unknown }) {
  const message = error instanceof ApiError ? error.message : error instanceof Error ? error.message : 'Die Anfrage ist fehlgeschlagen.'
  return <div className="auth-inline-error">{message}</div>
}
