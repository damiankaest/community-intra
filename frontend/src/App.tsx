import type { InputHTMLAttributes, ReactNode } from 'react'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import {
  useMutation,
  useQuery,
  useQueryClient,
  type UseQueryResult,
} from '@tanstack/react-query'
import {
  ArrowRight,
  Building2,
  Check,
  Clipboard,
  Copy,
  Crosshair,
  FolderTree,
  LogIn,
  LogOut,
  PartyPopper,
  Plus,
  RotateCcw,
  ShieldCheck,
  Sparkles,
  UserMinus,
  UserPlus,
  Users,
} from 'lucide-react'
import {
  Link,
  Navigate,
  Outlet,
  Route,
  Routes,
  useLocation,
  useNavigate,
  useParams,
} from 'react-router-dom'
import {
  getCurrentUser,
  login,
  logout,
  register,
  type CurrentUser,
  type LoginInput,
  type RegisterInput,
} from './api/auth'
import { ApiError } from './api/client'
import {
  acceptInvitation,
  archiveDepartment,
  createDepartment,
  createInvitation,
  listDepartments,
  listInvitations,
  listMembers,
  resolveInvitation,
  revokeInvitation,
  updateMember,
  type CreateInvitationInput,
  type Department,
  type Invitation,
  type Member,
  type UpdateMemberInput,
} from './api/members'
import {
  getOrganization,
  listOrganizations,
  type PermissionRole,
  type OrganizationSummary,
} from './api/organizations'
import { getThemePack, listThemePacks, type ThemePack } from './api/themePacks'
import { OrganizationWizard } from './components/OrganizationWizard'
import { OrganizationSwitcher } from './components/OrganizationSwitcher'
import {
  ActivitiesPage,
  AwardsPage,
  IncidentsPage,
  PhaseSixDashboard,
  ProjectsPage,
  TasksPage,
} from './components/PhaseSixPages'
import { AiAssistantPanel } from './components/AiAssistantPanel'
import { LiveOperationsPage } from './components/LiveOperationsPage'
import { TimeClockPage } from './components/TimeClockPage'
import { ThemeIcon } from './components/ThemeIcon'
import { PartyGuestPage } from './components/PartyGuestPage'
import { PartyAdminPage, PartyListPage } from './components/PartyAdminPage'
import {
  CounterStrikeApp,
  CounterStrikeEntry,
} from './components/CounterStrikeApp'
import {
  AccountPage,
  ExternalLoginButtons,
  ForgotPasswordPage,
  ResetPasswordPage,
} from './components/AuthAccountPages'
import { applyTheme, getThemeCssVariables, resetTheme } from './theme'
import { invitationReturnPath } from './invitationRoutes'
import { cs2Path } from './components/counterStrikeRoutes'

const currentUserKey = ['current-user'] as const
const organizationsKey = ['organizations'] as const
const pendingInvitationKey = 'community-pending-invitation'
const pendingInvitationReturnToKey = 'community-pending-invitation-return-to'

function App() {
  const currentUser = useQuery({
    queryKey: currentUserKey,
    queryFn: getCurrentUser,
    retry: false,
  })

  return (
    <Routes>
      <Route path="/" element={<LandingPage user={currentUser.data} />} />
      <Route
        path="/login"
        element={
          currentUser.data ? (
            <Navigate to="/organizations" replace />
          ) : (
            <AuthPage mode="login" />
          )
        }
      />
      <Route
        path="/register"
        element={
          currentUser.data ? (
            <Navigate to="/organizations" replace />
          ) : (
            <AuthPage mode="register" />
          )
        }
      />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />
      <Route
        path="/account"
        element={
          <ProtectedRoute query={currentUser}>
            <AccountPage user={currentUser.data!} />
          </ProtectedRoute>
        }
      />
      <Route
        path="/cs2"
        element={
          <ProtectedRoute query={currentUser}>
            <CounterStrikeEntry />
          </ProtectedRoute>
        }
      />
      <Route
        path="/cs2/:organizationId/*"
        element={
          <ProtectedRoute query={currentUser}>
            <CounterStrikeApp user={currentUser.data!} />
          </ProtectedRoute>
        }
      />
      <Route
        path="/invite"
        element={
          <InvitationPage
            user={currentUser.data}
            isCheckingSession={currentUser.isPending}
          />
        }
      />
      <Route path="/party/:slug" element={<PartyGuestPage />} />
      <Route
        path="/parties"
        element={
          <ProtectedRoute query={currentUser}>
            <PartyListPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/parties/:partyId"
        element={
          <ProtectedRoute query={currentUser}>
            <PartyAdminPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/organizations"
        element={
          <ProtectedRoute query={currentUser}>
            <OrganizationListPage user={currentUser.data!} />
          </ProtectedRoute>
        }
      />
      <Route
        path="/organizations/new"
        element={
          <ProtectedRoute query={currentUser}>
            <CreateOrganizationPage user={currentUser.data!} />
          </ProtectedRoute>
        }
      />
      <Route
        path="/organizations/:organizationId"
        element={
          <ProtectedRoute query={currentUser}>
            <OrganizationAssistantBoundary />
          </ProtectedRoute>
        }
      >
        <Route index element={<PhaseSixDashboard user={currentUser.data!} />} />
        <Route
          path="projects"
          element={<ProjectsPage user={currentUser.data!} />}
        />
        <Route path="tasks" element={<TasksPage user={currentUser.data!} />} />
        <Route
          path="incidents"
          element={<IncidentsPage user={currentUser.data!} />}
        />
        <Route
          path="awards"
          element={<AwardsPage user={currentUser.data!} />}
        />
        <Route
          path="activities"
          element={<ActivitiesPage user={currentUser.data!} />}
        />
        <Route
          path="server"
          element={<LiveOperationsPage user={currentUser.data!} />}
        />
        <Route
          path="time-clock"
          element={<TimeClockPage user={currentUser.data!} />}
        />
        <Route
          path="members"
          element={<MembersPage user={currentUser.data!} />}
        />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

function OrganizationAssistantBoundary() {
  const { organizationId = '' } = useParams()
  const organization = useQuery({
    queryKey: ['organization', organizationId],
    queryFn: () => getOrganization(organizationId),
    enabled: Boolean(organizationId),
  })
  const themePack = useQuery({
    queryKey: ['theme-pack', organization.data?.themePackKey],
    queryFn: () => getThemePack(organization.data!.themePackKey),
    enabled: Boolean(organization.data?.themePackKey),
  })

  return (
    <>
      <Outlet />
      <AiAssistantPanel
        key={organizationId}
        organizationId={organizationId}
        themeName={themePack.data?.name}
      />
    </>
  )
}

interface ProtectedRouteProps {
  query: UseQueryResult<CurrentUser, Error>
  children: ReactNode
}

function ProtectedRoute({ query, children }: ProtectedRouteProps) {
  const location = useLocation()

  if (query.isPending) {
    return <FullPageMessage message="Sitzung wird geprüft …" />
  }

  if (!query.data) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  return children
}

function LandingPage({ user }: { user?: CurrentUser }) {
  return (
    <div className="min-h-screen overflow-hidden bg-[var(--theme-background)] text-[var(--theme-text)]">
      <div className="industrial-grid pointer-events-none fixed inset-0 opacity-50" />
      <PublicHeader user={user} />

      <main className="relative mx-auto grid min-h-[calc(100vh-73px)] max-w-7xl items-center gap-14 px-5 py-16 sm:px-8 lg:grid-cols-[1.15fr_0.85fr]">
        <section>
          <div className="inline-flex items-center gap-2 rounded-full border border-[var(--theme-primary)]/25 bg-[var(--theme-primary)]/10 px-3 py-1.5 text-xs font-semibold tracking-[0.14em] text-[var(--theme-primary)] uppercase">
            <ShieldCheck size={14} />
            Mandantenfähig · selbst gehostet
          </div>
          <h1 className="mt-7 max-w-4xl text-5xl leading-[1.02] font-black tracking-[-0.05em] text-white sm:text-7xl">
            Euer eigenes Intranet.
            <span className="block text-[var(--theme-primary)]">
              Erfreulich überorganisiert.
            </span>
          </h1>
          <p className="mt-7 max-w-2xl text-base leading-7 text-[var(--theme-muted)] sm:text-lg">
            Erstellt einen sicheren Bereich für eure Freundesgruppe, euren
            Verein oder Gaming-Server. Technische Rollen bleiben seriös – die
            sichtbaren Jobtitel dürfen es ausdrücklich nicht sein.
          </p>
          <div className="mt-9 flex flex-wrap gap-3">
            <PrimaryLink to={user ? '/organizations' : '/register'}>
              {user ? 'Zu deinen Organisationen' : 'Intranet gründen'}
              <ArrowRight size={17} />
            </PrimaryLink>
            {!user && (
              <SecondaryLink to="/login">
                <LogIn size={17} />
                Anmelden
              </SecondaryLink>
            )}
          </div>
        </section>

        <section className="relative">
          <div className="absolute -inset-10 rounded-full bg-[var(--theme-primary)]/10 blur-3xl" />
          <div className="relative overflow-hidden rounded-3xl border border-white/10 bg-[var(--theme-surface)] shadow-2xl shadow-black/30">
            <div className="border-b border-white/10 p-6">
              <p className="text-xs font-bold tracking-[0.16em] text-[var(--theme-primary)] uppercase">
                Phase 10
              </p>
              <h2 className="mt-2 text-2xl font-bold text-white">
                Ein Kern, viele Welten
              </h2>
            </div>
            <div className="grid gap-3 p-6">
              <Feature
                icon={<ShieldCheck />}
                text="Sichere Anmeldung mit Token-Rotation"
              />
              <Feature
                icon={<Building2 />}
                text="Mehrere getrennte Organisationen"
              />
              <Feature icon={<Users />} text="Owner-Rolle bei der Gründung" />
              <Feature
                icon={<UserPlus />}
                text="Sichere Einladungslinks für eure Freunde"
              />
              <Feature
                icon={<Sparkles />}
                text="KI-Arbeitspläne im Theme- oder Normal-Ton"
              />
            </div>
          </div>
        </section>
      </main>
    </div>
  )
}

function PublicHeader({ user }: { user?: CurrentUser }) {
  return (
    <header className="relative border-b border-white/10 bg-black/20 backdrop-blur">
      <div className="mx-auto flex max-w-7xl items-center justify-between px-5 py-4 sm:px-8">
        <a href="/" className="flex items-center gap-3 text-white">
          <Logo />
          <div>
            <p className="text-sm font-semibold">Community Intranet</p>
            <p className="text-xs text-[var(--theme-muted)]">
              Live Operations & Zusammenarbeit · Phase 10
            </p>
          </div>
        </a>
        {user && (
          <PrimaryLink to="/organizations">
            {user.displayName}
            <ArrowRight size={16} />
          </PrimaryLink>
        )}
      </div>
    </header>
  )
}

function AuthPage({ mode }: { mode: 'login' | 'register' }) {
  const isRegister = mode === 'register'
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const location = useLocation()
  const requestedPath = (
    location.state as { from?: { pathname?: string; search?: string } } | null
  )?.from
  const returnUrl = requestedPath?.pathname
    ? `${requestedPath.pathname}${requestedPath.search ?? ''}`
    : '/organizations'
  const authStatus = new URLSearchParams(location.search).get('auth')
  const resetCompleted = new URLSearchParams(location.search).get('reset') === 'success'
  const form = useForm<RegisterInput>({
    defaultValues: {
      displayName: '',
      email: '',
      password: '',
    },
  })
  const mutation = useMutation({
    mutationFn: (values: RegisterInput) =>
      isRegister ? register(values) : login(values satisfies LoginInput),
    onSuccess: (response) => {
      queryClient.setQueryData(currentUserKey, response.user)
      navigate(
        sessionStorage.getItem(pendingInvitationKey)
          ? '/invite'
          : returnUrl,
        { replace: true },
      )
    },
  })

  const submit = form.handleSubmit((values) => mutation.mutate(values))

  return (
    <CenteredPanel>
      <div className="mb-8 flex items-center gap-3">
        <Logo />
        <div>
          <p className="font-semibold text-white">Community Intranet</p>
          <p className="text-sm text-[var(--theme-muted)]">
            {isRegister
              ? 'Neue Personalakte anlegen'
              : 'Zutritt zur Niederlassung'}
          </p>
        </div>
      </div>

      <h1 className="text-3xl font-black tracking-tight text-white">
        {isRegister ? 'Konto erstellen' : 'Willkommen zurück'}
      </h1>
      <p className="mt-3 text-sm leading-6 text-[var(--theme-muted)]">
        {isRegister
          ? 'Ein Konto kann später Mitglied mehrerer Organisationen sein.'
          : 'Melde dich mit deiner E-Mail-Adresse und deinem Passwort an.'}
      </p>

      {resetCompleted && <div className="auth-success"><Check /> Passwort geändert. Du kannst dich jetzt anmelden.</div>}
      {authStatus && <div className="auth-inline-error">{externalAuthMessage(authStatus)}</div>}

      {!isRegister && <ExternalLoginButtons returnUrl={returnUrl} />}

      <form className="mt-8 space-y-5" onSubmit={submit}>
        {isRegister && (
          <TextField
            label="Anzeigename"
            autoComplete="name"
            error={form.formState.errors.displayName?.message}
            {...form.register('displayName', {
              required: 'Bitte gib deinen Anzeigenamen ein.',
              minLength: { value: 2, message: 'Mindestens 2 Zeichen.' },
            })}
          />
        )}
        <TextField
          label="E-Mail-Adresse"
          type="email"
          autoComplete="email"
          error={form.formState.errors.email?.message}
          {...form.register('email', {
            required: 'Bitte gib deine E-Mail-Adresse ein.',
          })}
        />

        {!isRegister && (
          <div className="-mt-2 text-right">
            <Link
              className="text-sm font-semibold text-[var(--theme-primary)] hover:underline"
              to="/forgot-password"
            >
              Passwort vergessen?
            </Link>
          </div>
        )}
        <TextField
          label="Passwort"
          type="password"
          autoComplete={isRegister ? 'new-password' : 'current-password'}
          hint={
            isRegister
              ? 'Mindestens 12 Zeichen, Groß-/Kleinbuchstabe und Zahl.'
              : undefined
          }
          error={form.formState.errors.password?.message}
          {...form.register('password', {
            required: 'Bitte gib dein Passwort ein.',
            minLength: isRegister
              ? { value: 12, message: 'Mindestens 12 Zeichen.' }
              : undefined,
          })}
        />

        {mutation.error && <ErrorNotice error={mutation.error} />}

        <button
          type="submit"
          disabled={mutation.isPending}
          className="flex h-12 w-full items-center justify-center gap-2 rounded-xl bg-[var(--theme-primary)] px-5 font-bold text-black transition hover:bg-amber-400 disabled:cursor-wait disabled:opacity-60"
        >
          {mutation.isPending
            ? 'Wird geprüft …'
            : isRegister
              ? 'Konto erstellen'
              : 'Anmelden'}
          <ArrowRight size={17} />
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-[var(--theme-muted)]">
        {isRegister ? 'Schon registriert?' : 'Noch kein Konto?'}{' '}
        <a
          className="font-semibold text-[var(--theme-primary)] hover:underline"
          href={isRegister ? '/login' : '/register'}
        >
          {isRegister ? 'Anmelden' : 'Jetzt registrieren'}
        </a>
      </p>
    </CenteredPanel>
  )
}

function externalAuthMessage(status: string) {
  switch (status) {
    case 'verified_email_required': return 'Der Anbieter hat keine bestätigte E-Mail-Adresse geliefert.'
    case 'account_disabled': return 'Dieses Konto ist deaktiviert.'
    case 'external_conflict':
    case 'external_create_failed': return 'Das externe Konto konnte nicht sicher zugeordnet werden.'
    default: return 'Die externe Anmeldung konnte nicht abgeschlossen werden.'
  }
}

function InvitationPage({
  user,
  isCheckingSession,
}: {
  user?: CurrentUser
  isCheckingSession: boolean
}) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [token] = useState(() => {
    const hashToken = window.location.hash.replace(/^#/, '').trim()
    return hashToken || sessionStorage.getItem(pendingInvitationKey) || ''
  })
  const [requestedReturnTo] = useState(() =>
    new URLSearchParams(window.location.search).get('returnTo')
      ?? sessionStorage.getItem(pendingInvitationReturnToKey)
      ?? '',
  )
  const invitation = useQuery({
    queryKey: ['invitation-preview', token],
    queryFn: () => resolveInvitation(token),
    enabled: Boolean(token),
    retry: false,
  })
  const themePack = useQuery({
    queryKey: ['theme-pack', invitation.data?.themePackKey],
    queryFn: () => getThemePack(invitation.data!.themePackKey),
    enabled: Boolean(user && invitation.data?.themePackKey),
  })
  const acceptMutation = useMutation({
    mutationFn: () => acceptInvitation(token),
    onSuccess: async (result) => {
      sessionStorage.removeItem(pendingInvitationKey)
      sessionStorage.removeItem(pendingInvitationReturnToKey)
      await queryClient.invalidateQueries({ queryKey: organizationsKey })
      navigate(
        invitationReturnPath(requestedReturnTo, result.organizationId),
        { replace: true },
      )
    },
  })

  useEffect(() => {
    if (token) {
      sessionStorage.setItem(pendingInvitationKey, token)
    }
  }, [token])

  useEffect(() => {
    if (requestedReturnTo) {
      sessionStorage.setItem(
        pendingInvitationReturnToKey,
        requestedReturnTo,
      )
    }
  }, [requestedReturnTo])

  useEffect(() => {
    applyTheme(themePack.data)
    return resetTheme
  }, [themePack.data])

  return (
    <CenteredPanel>
      <div className="mb-8 flex items-center gap-3">
        <Logo icon={themePack.data?.configuration.visuals.logoIcon} />
        <div>
          <p className="font-semibold text-white">Community Intranet</p>
          <p className="text-sm text-[var(--theme-muted)]">
            Offizielle Einladung
          </p>
        </div>
      </div>

      {!token && (
        <ErrorNotice
          error={new Error('Der Einladungslink ist unvollständig.')}
        />
      )}
      {invitation.isPending && token && (
        <InlineMessage message="Einladung wird geprüft …" />
      )}
      {invitation.error && <ErrorNotice error={invitation.error} />}
      {invitation.data && (
        <>
          <p className="text-xs font-bold tracking-[0.16em] text-[var(--theme-primary)] uppercase">
            Personalzugang genehmigt
          </p>
          <h1 className="mt-3 text-3xl font-black tracking-tight text-white">
            {invitation.data.organizationName}
          </h1>
          <p className="mt-4 text-sm leading-6 text-[var(--theme-muted)]">
            Du wurdest als{' '}
            <strong className="text-white">
              {invitation.data.defaultPermissionRole}
            </strong>{' '}
            eingeladen. Der Link kann noch{' '}
            {invitation.data.remainingUses === 1
              ? 'einmal'
              : `${invitation.data.remainingUses}-mal`}{' '}
            verwendet werden.
          </p>
          <p className="mt-2 text-xs text-[var(--theme-muted)]">
            Gültig bis{' '}
            {new Date(invitation.data.expiresAt).toLocaleString('de-DE')}.
          </p>

          {isCheckingSession ? (
            <InlineMessage message="Sitzung wird geprüft …" />
          ) : user ? (
            <div className="mt-8">
              {acceptMutation.error && (
                <ErrorNotice error={acceptMutation.error} />
              )}
              <button
                type="button"
                onClick={() => acceptMutation.mutate()}
                disabled={acceptMutation.isPending}
                className="mt-4 flex h-12 w-full items-center justify-center gap-2 rounded-xl bg-[var(--theme-primary)] px-5 font-bold text-black transition hover:brightness-110 disabled:opacity-60"
              >
                <UserPlus size={18} />
                {acceptMutation.isPending
                  ? 'Beitritt wird verbucht …'
                  : `Als ${user.displayName} beitreten`}
              </button>
            </div>
          ) : (
            <div className="mt-8 grid gap-3 sm:grid-cols-2">
              <PrimaryLink to="/register">Konto erstellen</PrimaryLink>
              <SecondaryLink to="/login">Anmelden</SecondaryLink>
            </div>
          )}
        </>
      )}
    </CenteredPanel>
  )
}

function OrganizationListPage({ user }: { user: CurrentUser }) {
  const organizations = useQuery({
    queryKey: organizationsKey,
    queryFn: listOrganizations,
  })
  const themePacks = useQuery({
    queryKey: ['theme-packs'],
    queryFn: listThemePacks,
  })
  const themeByKey = new Map(
    themePacks.data?.map((theme) => [theme.key, theme]) ?? [],
  )

  return (
    <AppShell user={user}>
      <div className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <p className="text-xs font-bold tracking-[0.16em] text-[var(--theme-primary)] uppercase">
            Organisationsauswahl
          </p>
          <h1 className="mt-2 text-4xl font-black tracking-tight text-white">
            Deine Intranets
          </h1>
          <p className="mt-3 text-[var(--theme-muted)]">
            Jede Organisation ist ein getrennt geprüfter Mandant.
          </p>
        </div>
        <PrimaryLink to="/organizations/new">
          <Plus size={17} />
          Organisation erstellen
        </PrimaryLink>
      </div>

      {organizations.isPending && (
        <InlineMessage message="Organisationen werden geladen …" />
      )}
      {organizations.error && <ErrorNotice error={organizations.error} />}
      {organizations.data?.length === 0 && (
        <div className="mt-10 rounded-2xl border border-dashed border-white/15 bg-white/[0.025] p-10 text-center">
          <Building2
            className="mx-auto text-[var(--theme-primary)]"
            size={34}
          />
          <h2 className="mt-4 text-xl font-bold text-white">
            Noch keine Niederlassung aktenkundig
          </h2>
          <p className="mt-2 text-sm text-[var(--theme-muted)]">
            Die Gründung dauert weniger lang als die erste Betriebsversammlung.
          </p>
        </div>
      )}

      <div className="mt-10 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {organizations.data?.map((organization) => (
          <OrganizationCard
            key={organization.id}
            organization={organization}
            theme={themeByKey.get(organization.themePackKey)}
          />
        ))}
      </div>
    </AppShell>
  )
}

function OrganizationCard({
  organization,
  theme,
}: {
  organization: OrganizationSummary
  theme?: ThemePack
}) {
  const visuals = theme?.configuration.visuals
  const style = visuals ? getThemeCssVariables(visuals) : undefined

  return (
    <a
      href={`/organizations/${organization.id}`}
      style={style}
      className="group rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-6 transition hover:-translate-y-0.5 hover:border-[var(--theme-primary)]/35"
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex size-11 items-center justify-center rounded-xl bg-[var(--theme-primary)]/10 text-[var(--theme-primary)]">
          <ThemeIcon name={visuals?.logoIcon ?? 'building-2'} />
        </div>
        <span className="rounded-full border border-white/10 px-2.5 py-1 text-xs text-[var(--theme-muted)]">
          {organization.permissionRole}
        </span>
      </div>
      <h2 className="mt-5 text-xl font-bold text-white group-hover:text-[var(--theme-primary)]">
        {organization.name}
      </h2>
      <p className="mt-2 line-clamp-2 text-sm leading-6 text-[var(--theme-muted)]">
        {organization.description ?? 'Noch keine Beschreibung hinterlegt.'}
      </p>
      <div className="mt-5 flex items-center justify-between gap-3">
        <span className="text-xs font-semibold text-[var(--theme-primary)]">
          {organization.visibleTitle ??
            theme?.name ??
            organization.themePackKey}
        </span>
        <span className="text-xs text-[var(--theme-muted)]">
          v{organization.themePackVersion}
        </span>
      </div>
    </a>
  )
}

function CreateOrganizationPage({ user }: { user: CurrentUser }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  return (
    <AppShell user={user}>
      <div className="mx-auto max-w-5xl">
        <p className="text-xs font-bold tracking-[0.16em] text-[var(--theme-primary)] uppercase">
          Gründungsformular A-38
        </p>
        <h1 className="mt-2 text-4xl font-black tracking-tight text-white">
          Organisation erstellen
        </h1>
        <p className="mt-3 leading-7 text-[var(--theme-muted)]">
          Wähle Farben, Begriffe und den passenden Grad kreativer Bürokratie. Du
          wirst automatisch Owner.
        </p>
        <OrganizationWizard
          onCreated={async (organizationId) => {
            await queryClient.invalidateQueries({ queryKey: organizationsKey })
            navigate(`/organizations/${organizationId}`, { replace: true })
          }}
        />
      </div>
    </AppShell>
  )
}

export function OrganizationDashboard({ user }: { user: CurrentUser }) {
  const { organizationId = '' } = useParams()
  const navigate = useNavigate()
  const organizations = useQuery({
    queryKey: organizationsKey,
    queryFn: listOrganizations,
  })
  const organization = useQuery({
    queryKey: ['organization', organizationId],
    queryFn: () => getOrganization(organizationId),
    enabled: Boolean(organizationId),
  })
  const themePack = useQuery({
    queryKey: ['theme-pack', organization.data?.themePackKey],
    queryFn: () => getThemePack(organization.data!.themePackKey),
    enabled: Boolean(organization.data?.themePackKey),
  })

  useEffect(() => {
    applyTheme(themePack.data)
    return resetTheme
  }, [themePack.data])

  const terminology = themePack.data?.configuration.terminology
  const messages = themePack.data?.configuration.messages
  const visuals = themePack.data?.configuration.visuals

  return (
    <AppShell user={user} theme={themePack.data}>
      <div className="flex flex-col gap-5 border-b border-white/10 pb-8 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-xs font-bold tracking-[0.16em] text-[var(--theme-primary)] uppercase">
            Aktive {terminology?.organization ?? 'Organisation'}
          </p>
          <h1 className="mt-2 text-4xl font-black tracking-tight text-white">
            {organization.data?.name ?? 'Organisation wird geladen …'}
          </h1>
          <p className="mt-3 text-[var(--theme-muted)]">
            {organization.data?.description ??
              'Noch keine offizielle Selbstdarstellung hinterlegt.'}
          </p>
        </div>
        {organizations.data && (
          <div className="flex flex-col gap-3 sm:flex-row">
            <OrganizationSwitcher
              organizations={organizations.data}
              selectedId={organizationId}
              onSelect={(id) => navigate(`/organizations/${id}`)}
            />
            <PrimaryLink to={`/organizations/${organizationId}/members`}>
              <Users size={17} />
              {terminology?.members ?? 'Mitglieder'}
            </PrimaryLink>
          </div>
        )}
      </div>

      {organization.error && <ErrorNotice error={organization.error} />}
      {themePack.error && <ErrorNotice error={themePack.error} />}
      {organization.data && (
        <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <InfoCard
            label="Technische Rolle"
            value={organization.data.permissionRole}
          />
          <InfoCard
            label="Sichtbarer Titel"
            value={organization.data.visibleTitle ?? 'Noch streng geheim'}
          />
          <InfoCard label="Sprache" value={organization.data.language} />
          <InfoCard label="Zeitzone" value={organization.data.timeZone} />
        </div>
      )}

      <div className="mt-8 rounded-2xl border border-white/10 bg-white/[0.03] p-7">
        <Sparkles className="text-[var(--theme-primary)]" size={24} />
        <h2 className="mt-4 text-xl font-bold text-white">
          {messages?.welcome ?? 'Der Mandant ist einsatzbereit'}
        </h2>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-[var(--theme-muted)]">
          Theme: {themePack.data?.name ?? 'wird geladen'} · Stil:{' '}
          {visuals?.style ?? 'wird geladen'} · Version:{' '}
          {organization.data?.themePackVersion ?? 'wird geladen'}
        </p>
      </div>

      {organization.data && terminology && messages && (
        <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {[
            {
              key: 'projects',
              label: terminology.project,
              message: messages.emptyProjects,
            },
            {
              key: 'tasks',
              label: terminology.task,
              message: messages.emptyTasks,
            },
            {
              key: 'incidents',
              label: terminology.incident,
              message: messages.emptyIncidents,
            },
            {
              key: 'activity-feed',
              label: terminology.activityFeed,
              message: messages.emptyActivityFeed,
            },
          ]
            .filter((module) =>
              organization.data.enabledModules.includes(module.key),
            )
            .map((module) => (
              <div
                key={module.key}
                className="rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5"
              >
                <p className="font-bold text-white">{module.label}</p>
                <p className="mt-2 text-sm leading-6 text-[var(--theme-muted)]">
                  {module.message}
                </p>
              </div>
            ))}
        </div>
      )}
    </AppShell>
  )
}

function MembersPage({ user }: { user: CurrentUser }) {
  const { organizationId = '' } = useParams()
  const queryClient = useQueryClient()
  const organization = useQuery({
    queryKey: ['organization', organizationId],
    queryFn: () => getOrganization(organizationId),
    enabled: Boolean(organizationId),
  })
  const themePack = useQuery({
    queryKey: ['theme-pack', organization.data?.themePackKey],
    queryFn: () => getThemePack(organization.data!.themePackKey),
    enabled: Boolean(organization.data?.themePackKey),
  })
  const members = useQuery({
    queryKey: ['members', organizationId],
    queryFn: () => listMembers(organizationId),
    enabled: Boolean(organizationId),
  })
  const departments = useQuery({
    queryKey: ['departments', organizationId],
    queryFn: () => listDepartments(organizationId),
    enabled: Boolean(organizationId),
  })
  const canManage =
    organization.data?.permissionRole === 'Owner' ||
    organization.data?.permissionRole === 'Administrator'
  const invitations = useQuery({
    queryKey: ['invitations', organizationId],
    queryFn: () => listInvitations(organizationId),
    enabled: Boolean(organizationId) && canManage,
  })
  const invitationForm = useForm<CreateInvitationInput>({
    defaultValues: {
      defaultPermissionRole: 'Member',
      expiresInDays: 7,
      maximumUses: 1,
    },
  })
  const departmentForm = useForm<{
    name: string
    description: string
    icon: string
  }>({
    defaultValues: { name: '', description: '', icon: 'users' },
  })
  const [createdLink, setCreatedLink] = useState('')
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    applyTheme(themePack.data)
    return resetTheme
  }, [themePack.data])

  const invitationMutation = useMutation({
    mutationFn: (input: CreateInvitationInput) =>
      createInvitation(organizationId, input),
    onSuccess: async (created) => {
      setCreatedLink(`${window.location.origin}/invite#${created.token}`)
      setCopied(false)
      await queryClient.invalidateQueries({
        queryKey: ['invitations', organizationId],
      })
    },
  })
  const departmentMutation = useMutation({
    mutationFn: (values: { name: string; description: string; icon: string }) =>
      createDepartment(organizationId, {
        name: values.name,
        description: values.description || undefined,
        icon: values.icon,
      }),
    onSuccess: async () => {
      departmentForm.reset()
      await queryClient.invalidateQueries({
        queryKey: ['departments', organizationId],
      })
    },
  })
  const archiveDepartmentMutation = useMutation({
    mutationFn: (departmentId: string) =>
      archiveDepartment(organizationId, departmentId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ['departments', organizationId],
        }),
        queryClient.invalidateQueries({
          queryKey: ['members', organizationId],
        }),
      ])
    },
  })
  const revokeInvitationMutation = useMutation({
    mutationFn: (invitationId: string) =>
      revokeInvitation(organizationId, invitationId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['invitations', organizationId],
      })
    },
  })

  const terminology = themePack.data?.configuration.terminology

  return (
    <AppShell user={user} theme={themePack.data}>
      <a
        href={`/organizations/${organizationId}`}
        className="text-sm font-semibold text-[var(--theme-primary)] hover:underline"
      >
        ← Zurück zum Dashboard
      </a>
      <div className="mt-5 flex flex-col gap-5 border-b border-white/10 pb-8 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-xs font-bold tracking-[0.16em] text-[var(--theme-primary)] uppercase">
            Personalverwaltung
          </p>
          <h1 className="mt-2 text-4xl font-black tracking-tight text-white">
            {terminology?.members ?? 'Mitglieder'}
          </h1>
          <p className="mt-3 text-[var(--theme-muted)]">
            {organization.data?.name ?? 'Organisation wird geladen …'} ·{' '}
            technische Rollen und kreative Jobtitel sauber getrennt.
          </p>
        </div>
        {canManage && (
          <a
            href="#einladungen"
            className="inline-flex h-11 items-center justify-center gap-2 rounded-xl bg-[var(--theme-primary)] px-5 text-sm font-bold text-black"
          >
            <UserPlus size={17} />
            Person einladen
          </a>
        )}
      </div>

      {organization.error && <ErrorNotice error={organization.error} />}
      {members.error && <ErrorNotice error={members.error} />}
      {departments.error && <ErrorNotice error={departments.error} />}

      <section className="mt-8">
        <div className="flex items-center justify-between gap-4">
          <h2 className="text-xl font-bold text-white">Mitgliederliste</h2>
          <span className="text-sm text-[var(--theme-muted)]">
            {members.data?.filter((member) => member.isActive).length ?? 0}{' '}
            aktiv
          </span>
        </div>
        {members.isPending && (
          <InlineMessage message="Mitglieder werden geladen …" />
        )}
        <div className="mt-4 grid gap-4 xl:grid-cols-2">
          {members.data?.map((member) => (
            <MemberCard
              key={member.id}
              member={member}
              departments={departments.data ?? []}
              organizationId={organizationId}
              canManage={canManage}
              managerRole={organization.data?.permissionRole}
              currentUserId={user.id}
            />
          ))}
        </div>
      </section>

      <section className="mt-12">
        <div className="flex items-center gap-3">
          <FolderTree className="text-[var(--theme-primary)]" size={22} />
          <h2 className="text-xl font-bold text-white">
            {terminology?.department ?? 'Abteilungen'}
          </h2>
        </div>
        <div className="mt-4 grid gap-4 lg:grid-cols-[1fr_360px]">
          <div className="grid content-start gap-3 sm:grid-cols-2">
            {departments.data?.map((department) => (
              <div
                key={department.id}
                className="rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5"
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="font-bold text-white">{department.name}</p>
                    <p className="mt-1 text-xs text-[var(--theme-muted)]">
                      Icon: {department.icon}
                    </p>
                  </div>
                  {canManage && (
                    <button
                      type="button"
                      onClick={() =>
                        archiveDepartmentMutation.mutate(department.id)
                      }
                      className="text-xs font-semibold text-rose-300 hover:underline"
                    >
                      Archivieren
                    </button>
                  )}
                </div>
                {department.description && (
                  <p className="mt-3 text-sm leading-6 text-[var(--theme-muted)]">
                    {department.description}
                  </p>
                )}
              </div>
            ))}
          </div>
          {canManage && (
            <form
              onSubmit={departmentForm.handleSubmit((values) =>
                departmentMutation.mutate(values),
              )}
              className="rounded-2xl border border-white/10 bg-white/[0.035] p-5"
            >
              <h3 className="font-bold text-white">Abteilung ergänzen</h3>
              <div className="mt-4 space-y-4">
                <TextField
                  label="Name"
                  error={departmentForm.formState.errors.name?.message}
                  {...departmentForm.register('name', {
                    required: 'Bitte gib einen Namen ein.',
                    maxLength: { value: 100, message: 'Maximal 100 Zeichen.' },
                  })}
                />
                <TextField
                  label="Beschreibung"
                  {...departmentForm.register('description')}
                />
                <TextField
                  label="Lucide-Icon"
                  {...departmentForm.register('icon', {
                    required: 'Bitte gib ein Icon ein.',
                  })}
                />
              </div>
              {departmentMutation.error && (
                <div className="mt-4">
                  <ErrorNotice error={departmentMutation.error} />
                </div>
              )}
              <button
                type="submit"
                disabled={departmentMutation.isPending}
                className="mt-5 flex h-11 w-full items-center justify-center gap-2 rounded-xl bg-white/10 text-sm font-bold text-white hover:bg-white/15 disabled:opacity-60"
              >
                <Plus size={16} />
                Anlegen
              </button>
            </form>
          )}
        </div>
      </section>

      {canManage && (
        <section id="einladungen" className="mt-12 scroll-mt-6">
          <div className="flex items-center gap-3">
            <UserPlus className="text-[var(--theme-primary)]" size={22} />
            <h2 className="text-xl font-bold text-white">Einladungen</h2>
          </div>
          <div className="mt-4 grid gap-4 lg:grid-cols-[380px_1fr]">
            <form
              onSubmit={invitationForm.handleSubmit((values) =>
                invitationMutation.mutate({
                  ...values,
                  expiresInDays: Number(values.expiresInDays),
                  maximumUses: Number(values.maximumUses),
                }),
              )}
              className="rounded-2xl border border-[var(--theme-primary)]/25 bg-[var(--theme-primary)]/[0.06] p-6"
            >
              <h3 className="font-bold text-white">Neuen Link erstellen</h3>
              <label className="mt-5 block">
                <span className="mb-2 block text-sm font-semibold text-white">
                  Technische Rolle
                </span>
                <select
                  {...invitationForm.register('defaultPermissionRole')}
                  className="h-12 w-full rounded-xl border border-white/10 bg-[var(--theme-background)] px-4 text-white outline-none"
                >
                  <option value="Guest">Guest</option>
                  <option value="Member">Member</option>
                  <option value="Moderator">Moderator</option>
                  {organization.data?.permissionRole === 'Owner' && (
                    <option value="Administrator">Administrator</option>
                  )}
                </select>
              </label>
              <div className="mt-4 grid grid-cols-2 gap-3">
                <TextField
                  label="Gültig (Tage)"
                  type="number"
                  min={1}
                  max={30}
                  {...invitationForm.register('expiresInDays', {
                    valueAsNumber: true,
                  })}
                />
                <TextField
                  label="Nutzungen"
                  type="number"
                  min={1}
                  max={100}
                  {...invitationForm.register('maximumUses', {
                    valueAsNumber: true,
                  })}
                />
              </div>
              {invitationMutation.error && (
                <div className="mt-4">
                  <ErrorNotice error={invitationMutation.error} />
                </div>
              )}
              <button
                type="submit"
                disabled={invitationMutation.isPending}
                className="mt-5 flex h-12 w-full items-center justify-center gap-2 rounded-xl bg-[var(--theme-primary)] font-bold text-black disabled:opacity-60"
              >
                <UserPlus size={17} />
                Einladungslink erstellen
              </button>
            </form>

            <div>
              {createdLink && (
                <div className="mb-4 rounded-2xl border border-emerald-400/25 bg-emerald-400/10 p-5">
                  <div className="flex items-center gap-2 font-bold text-emerald-200">
                    <Check size={18} />
                    Link bereit zum Teilen
                  </div>
                  <p className="mt-2 text-sm break-all text-emerald-100/80">
                    {createdLink}
                  </p>
                  <button
                    type="button"
                    onClick={async () => {
                      await navigator.clipboard.writeText(createdLink)
                      setCopied(true)
                    }}
                    className="mt-4 inline-flex h-10 items-center gap-2 rounded-xl bg-emerald-300 px-4 text-sm font-bold text-emerald-950"
                  >
                    {copied ? <Check size={16} /> : <Copy size={16} />}
                    {copied ? 'Kopiert' : 'Link kopieren'}
                  </button>
                  <p className="mt-3 text-xs text-emerald-100/65">
                    Der vollständige Link wird aus Sicherheitsgründen nur jetzt
                    angezeigt.
                  </p>
                </div>
              )}
              {invitations.error && <ErrorNotice error={invitations.error} />}
              <div className="space-y-3">
                {invitations.data?.map((invitation) => (
                  <InvitationCard
                    key={invitation.id}
                    invitation={invitation}
                    onRevoke={() =>
                      revokeInvitationMutation.mutate(invitation.id)
                    }
                  />
                ))}
                {invitations.data?.length === 0 && (
                  <div className="rounded-2xl border border-dashed border-white/15 p-8 text-center text-sm text-[var(--theme-muted)]">
                    Noch keine Einladungen erstellt.
                  </div>
                )}
              </div>
            </div>
          </div>
        </section>
      )}
    </AppShell>
  )
}

function MemberCard({
  member,
  departments,
  organizationId,
  canManage,
  managerRole,
  currentUserId,
}: {
  member: Member
  departments: Department[]
  organizationId: string
  canManage: boolean
  managerRole?: PermissionRole
  currentUserId: string
}) {
  const queryClient = useQueryClient()
  const form = useForm<UpdateMemberInput>({
    defaultValues: {
      permissionRole: member.permissionRole,
      visibleTitle: member.visibleTitle ?? '',
      departmentId: member.departmentId ?? '',
      statusMessage: member.statusMessage ?? '',
      isActive: member.isActive,
    },
  })
  const mutation = useMutation({
    mutationFn: (input: UpdateMemberInput) =>
      updateMember(organizationId, member.id, {
        ...input,
        visibleTitle: input.visibleTitle || undefined,
        departmentId: input.departmentId || undefined,
        statusMessage: input.statusMessage || undefined,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ['members', organizationId],
      })
    },
  })
  const isOwner = member.permissionRole === 'Owner'
  const canEdit =
    canManage &&
    (managerRole === 'Owner' ||
      !['Owner', 'Administrator'].includes(member.permissionRole))
  const editableRoles: PermissionRole[] =
    managerRole === 'Owner'
      ? ['Guest', 'Member', 'Moderator', 'Administrator']
      : ['Guest', 'Member', 'Moderator']
  if (isOwner) {
    editableRoles.push('Owner')
  }

  return (
    <article
      className={`rounded-2xl border p-5 ${
        member.isActive
          ? 'border-white/10 bg-[var(--theme-surface)]'
          : 'border-white/[0.06] bg-white/[0.02] opacity-65'
      }`}
    >
      <div className="flex items-start gap-4">
        <div className="flex size-11 shrink-0 items-center justify-center rounded-full bg-[var(--theme-primary)]/15 font-black text-[var(--theme-primary)]">
          {member.displayName.slice(0, 1).toUpperCase()}
        </div>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="font-bold text-white">{member.displayName}</h3>
            <span className="rounded-full border border-white/10 px-2 py-0.5 text-[11px] text-[var(--theme-muted)]">
              {member.permissionRole}
            </span>
          </div>
          <p className="truncate text-xs text-[var(--theme-muted)]">
            {member.email}
          </p>
          {!canEdit && (
            <div className="mt-3 text-sm text-[var(--theme-muted)]">
              <p>{member.visibleTitle ?? 'Kein sichtbarer Titel'}</p>
              <p>{member.departmentName ?? 'Keine Abteilung'}</p>
            </div>
          )}
        </div>
      </div>

      {canEdit && (
        <form
          onSubmit={form.handleSubmit((values) => mutation.mutate(values))}
          className="mt-5 grid gap-3 border-t border-white/10 pt-5 sm:grid-cols-2"
        >
          <label>
            <span className="mb-1.5 block text-xs font-semibold text-[var(--theme-muted)]">
              Rolle
            </span>
            <select
              {...form.register('permissionRole')}
              disabled={isOwner}
              className="h-10 w-full rounded-xl border border-white/10 bg-[var(--theme-background)] px-3 text-sm text-white disabled:opacity-60"
            >
              {editableRoles.map((role) => (
                <option key={role} value={role}>
                  {role}
                </option>
              ))}
            </select>
          </label>
          <label>
            <span className="mb-1.5 block text-xs font-semibold text-[var(--theme-muted)]">
              Abteilung
            </span>
            <select
              {...form.register('departmentId')}
              className="h-10 w-full rounded-xl border border-white/10 bg-[var(--theme-background)] px-3 text-sm text-white"
            >
              <option value="">Keine Abteilung</option>
              {departments.map((department) => (
                <option key={department.id} value={department.id}>
                  {department.name}
                </option>
              ))}
            </select>
          </label>
          <label className="sm:col-span-2">
            <span className="mb-1.5 block text-xs font-semibold text-[var(--theme-muted)]">
              Sichtbarer Titel
            </span>
            <input
              {...form.register('visibleTitle')}
              maxLength={100}
              className="h-10 w-full rounded-xl border border-white/10 bg-black/15 px-3 text-sm text-white outline-none focus:border-[var(--theme-primary)]"
              placeholder="Chief Spaghetti Officer"
            />
          </label>
          <label className="sm:col-span-2">
            <span className="mb-1.5 block text-xs font-semibold text-[var(--theme-muted)]">
              Statusmeldung
            </span>
            <input
              {...form.register('statusMessage')}
              maxLength={280}
              className="h-10 w-full rounded-xl border border-white/10 bg-black/15 px-3 text-sm text-white outline-none focus:border-[var(--theme-primary)]"
              placeholder="Derzeit mit Förderbandangelegenheiten befasst"
            />
          </label>
          <div className="flex items-center gap-3 sm:col-span-2">
            <label className="flex flex-1 items-center gap-2 text-sm text-[var(--theme-muted)]">
              <input
                type="checkbox"
                {...form.register('isActive')}
                disabled={isOwner || member.userId === currentUserId}
                className="size-4 accent-[var(--theme-primary)]"
              />
              Mitglied aktiv
            </label>
            <button
              type="submit"
              disabled={mutation.isPending}
              className="inline-flex h-10 items-center gap-2 rounded-xl bg-white/10 px-4 text-sm font-bold text-white hover:bg-white/15 disabled:opacity-60"
            >
              <Clipboard size={15} />
              Speichern
            </button>
          </div>
          {mutation.error && (
            <div className="sm:col-span-2">
              <ErrorNotice error={mutation.error} />
            </div>
          )}
        </form>
      )}
    </article>
  )
}

function InvitationCard({
  invitation,
  onRevoke,
}: {
  invitation: Invitation
  onRevoke: () => void
}) {
  return (
    <div className="rounded-2xl border border-white/10 bg-[var(--theme-surface)] p-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <span
              className={`size-2 rounded-full ${
                invitation.isUsable ? 'bg-emerald-400' : 'bg-slate-500'
              }`}
            />
            <p className="font-bold text-white">
              {invitation.defaultPermissionRole}
            </p>
          </div>
          <p className="mt-1 text-xs text-[var(--theme-muted)]">
            Erstellt von {invitation.createdByDisplayName} ·{' '}
            {invitation.currentUses}/{invitation.maximumUses} genutzt
          </p>
          <p className="mt-1 text-xs text-[var(--theme-muted)]">
            Gültig bis {new Date(invitation.expiresAt).toLocaleString('de-DE')}
          </p>
        </div>
        {invitation.isUsable && (
          <button
            type="button"
            onClick={onRevoke}
            className="inline-flex items-center gap-2 text-xs font-semibold text-rose-300 hover:underline"
          >
            <UserMinus size={14} />
            Widerrufen
          </button>
        )}
        {!invitation.isUsable && (
          <span className="inline-flex items-center gap-1.5 text-xs text-[var(--theme-muted)]">
            <RotateCcw size={13} />
            Abgelaufen oder verbraucht
          </span>
        )}
      </div>
    </div>
  )
}

function AppShell({
  user,
  theme,
  children,
}: {
  user: CurrentUser
  theme?: ThemePack
  children: ReactNode
}) {
  const navigate = useNavigate()
  const { organizationId } = useParams()
  const queryClient = useQueryClient()
  const cs2Target = organizationId ? cs2Path(organizationId) : '/cs2'
  const logoutMutation = useMutation({
    mutationFn: logout,
    onSettled: () => {
      queryClient.clear()
      navigate('/', { replace: true })
    },
  })

  return (
    <div className="app-workspace min-h-screen bg-[var(--theme-background)] text-[var(--theme-text)]">
      <div className="industrial-grid pointer-events-none fixed inset-0 opacity-40" />
      <header className="relative border-b border-white/10 bg-black/25 backdrop-blur">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-2 px-3 py-3 sm:gap-4 sm:px-8 sm:py-4">
          <Link to="/organizations" className="flex shrink-0 items-center gap-3">
            <Logo icon={theme?.configuration.visuals.logoIcon} />
            <span className="hidden text-sm font-semibold text-white sm:inline">
              Community Intranet
            </span>
          </Link>
          <div className="flex min-w-0 items-center gap-1.5 sm:gap-3">
            <nav
              aria-label="Bereich wechseln"
              className="flex h-10 shrink-0 items-center rounded-xl border border-white/10 bg-black/25 p-1"
            >
              <span
                aria-current="page"
                className="flex h-8 items-center gap-1 rounded-lg bg-white/10 px-2 text-[10px] font-black text-white sm:gap-1.5 sm:px-3 sm:text-xs"
              >
                <Building2 size={14} />
                <span className="sm:hidden">Intra</span>
                <span className="hidden sm:inline">Intranet</span>
              </span>
              <Link
                to={cs2Target}
                className="flex h-8 items-center gap-1 rounded-lg px-2 text-[10px] font-black text-lime-200 transition hover:bg-lime-300/10 hover:text-white sm:gap-1.5 sm:px-3 sm:text-xs"
              >
                <Crosshair size={14} />
                CS2
              </Link>
            </nav>
            <Link
              to="/parties"
              className="flex h-10 items-center gap-2 rounded-xl border border-white/10 bg-white/5 px-3 text-xs font-bold text-[var(--theme-muted)] hover:text-white"
              aria-label="Partys öffnen"
            >
              <PartyPopper size={16} />
              <span className="hidden sm:inline">Partys</span>
            </Link>
            <Link to="/account" className="hidden text-right sm:block hover:opacity-80">
              <p className="text-sm font-semibold text-white">
                {user.displayName}
              </p>
              <p className="text-xs text-[var(--theme-muted)]">{user.email}</p>
            </Link>
            <button
              type="button"
              onClick={() => logoutMutation.mutate()}
              aria-label="Abmelden"
              className="flex size-10 items-center justify-center rounded-xl border border-white/10 bg-white/5 text-[var(--theme-muted)] hover:text-white"
            >
              <LogOut size={17} />
            </button>
          </div>
        </div>
      </header>
      <main className="relative mx-auto max-w-7xl px-5 py-10 sm:px-8 sm:py-14">
        {children}
      </main>
    </div>
  )
}

function TextField({
  label,
  error,
  hint,
  ...inputProps
}: InputHTMLAttributes<HTMLInputElement> & {
  label: string
  error?: string
  hint?: string
}) {
  const fieldId = inputProps.name ?? label
  return (
    <label className="block" htmlFor={fieldId}>
      <span className="mb-2 block text-sm font-semibold text-white">
        {label}
      </span>
      <input
        id={fieldId}
        {...inputProps}
        className="h-12 w-full rounded-xl border border-white/10 bg-black/20 px-4 text-white outline-none placeholder:text-slate-600 focus:border-[var(--theme-primary)]"
      />
      {error ? (
        <span className="mt-1.5 block text-xs text-[var(--theme-danger)]">
          {error}
        </span>
      ) : hint ? (
        <span className="mt-1.5 block text-xs leading-5 text-[var(--theme-muted)]">
          {hint}
        </span>
      ) : null}
    </label>
  )
}

function ErrorNotice({ error }: { error: Error }) {
  const message =
    error instanceof ApiError && error.status === 401
      ? 'E-Mail-Adresse oder Passwort ist nicht korrekt.'
      : error.message
  return (
    <div
      role="alert"
      className="rounded-xl border border-rose-400/20 bg-rose-400/10 px-4 py-3 text-sm text-rose-200"
    >
      {message}
    </div>
  )
}

function Logo({ icon = 'factory' }: { icon?: string }) {
  return (
    <span className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-[var(--theme-primary)] text-black">
      <ThemeIcon name={icon} />
    </span>
  )
}

function Feature({ icon, text }: { icon: ReactNode; text: string }) {
  return (
    <div className="flex items-center gap-4 rounded-xl border border-white/[0.07] bg-black/15 p-4">
      <span className="flex size-9 items-center justify-center text-[var(--theme-primary)] [&>svg]:size-5">
        {icon}
      </span>
      <span className="text-sm font-medium text-white">{text}</span>
    </div>
  )
}

function InfoCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-white/[0.035] p-5">
      <p className="text-xs text-[var(--theme-muted)]">{label}</p>
      <p className="mt-2 font-semibold text-white">{value}</p>
    </div>
  )
}

function CenteredPanel({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen bg-[var(--theme-background)] px-5 py-12">
      <div className="industrial-grid pointer-events-none fixed inset-0 opacity-40" />
      <main className="relative mx-auto max-w-md rounded-3xl border border-white/10 bg-[var(--theme-surface)] p-7 shadow-2xl shadow-black/30 sm:p-9">
        {children}
      </main>
    </div>
  )
}

function FullPageMessage({ message }: { message: string }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-[var(--theme-background)] text-[var(--theme-muted)]">
      {message}
    </div>
  )
}

function InlineMessage({ message }: { message: string }) {
  return <p className="mt-10 text-sm text-[var(--theme-muted)]">{message}</p>
}

function PrimaryLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <a
      href={to}
      className="inline-flex h-11 items-center justify-center gap-2 rounded-xl bg-[var(--theme-primary)] px-5 text-sm font-bold text-black transition hover:bg-amber-400"
    >
      {children}
    </a>
  )
}

function SecondaryLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <a
      href={to}
      className="inline-flex h-11 items-center justify-center gap-2 rounded-xl border border-white/10 bg-white/5 px-5 text-sm font-semibold text-white transition hover:border-white/20"
    >
      {children}
    </a>
  )
}

export default App
