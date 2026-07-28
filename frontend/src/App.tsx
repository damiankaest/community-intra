import type { InputHTMLAttributes, ReactNode } from 'react'
import { useEffect } from 'react'
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
  LogIn,
  LogOut,
  Plus,
  ShieldCheck,
  Sparkles,
  Users,
} from 'lucide-react'
import {
  Navigate,
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
  getOrganization,
  listOrganizations,
  type OrganizationSummary,
} from './api/organizations'
import { getThemePack, listThemePacks, type ThemePack } from './api/themePacks'
import { OrganizationWizard } from './components/OrganizationWizard'
import { OrganizationSwitcher } from './components/OrganizationSwitcher'
import { ThemeIcon } from './components/ThemeIcon'
import { applyTheme, getThemeCssVariables, resetTheme } from './theme'

const currentUserKey = ['current-user'] as const
const organizationsKey = ['organizations'] as const

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
            <OrganizationDashboard user={currentUser.data!} />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
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
                Phase 4
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
                icon={<Sparkles />}
                text="Theme Packs mit eigenen Farben und Begriffen"
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
              Theme Packs · Phase 4
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
      navigate('/organizations', { replace: true })
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

function OrganizationDashboard({ user }: { user: CurrentUser }) {
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
          <OrganizationSwitcher
            organizations={organizations.data}
            selectedId={organizationId}
            onSelect={(id) => navigate(`/organizations/${id}`)}
          />
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
  const queryClient = useQueryClient()
  const logoutMutation = useMutation({
    mutationFn: logout,
    onSettled: () => {
      queryClient.clear()
      navigate('/', { replace: true })
    },
  })

  return (
    <div className="min-h-screen bg-[var(--theme-background)] text-[var(--theme-text)]">
      <div className="industrial-grid pointer-events-none fixed inset-0 opacity-40" />
      <header className="relative border-b border-white/10 bg-black/25 backdrop-blur">
        <div className="mx-auto flex max-w-7xl items-center justify-between gap-4 px-5 py-4 sm:px-8">
          <a href="/organizations" className="flex items-center gap-3">
            <Logo icon={theme?.configuration.visuals.logoIcon} />
            <span className="hidden text-sm font-semibold text-white sm:inline">
              Community Intranet
            </span>
          </a>
          <div className="flex items-center gap-3">
            <div className="hidden text-right sm:block">
              <p className="text-sm font-semibold text-white">
                {user.displayName}
              </p>
              <p className="text-xs text-[var(--theme-muted)]">{user.email}</p>
            </div>
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
