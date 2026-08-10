import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type ChangeEvent,
  type ReactNode,
} from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Activity,
  Award,
  BarChart3,
  Check,
  ChevronRight,
  Clock3,
  Crosshair,
  Flame,
  Gamepad2,
  Home,
  Menu,
  RefreshCw,
  Shield,
  Sparkles,
  Swords,
  Target,
  Trophy,
  Upload,
  Users,
  X,
  Zap,
} from 'lucide-react'
import {
  Link,
  NavLink,
  Navigate,
  Route,
  Routes,
  useParams,
  useSearchParams,
} from 'react-router-dom'
import type { CurrentUser } from '../api/auth'
import {
  closeCs2Season,
  createCs2Season,
  getCs2Challenges,
  getCs2Dashboard,
  getCs2Leaderboards,
  getCs2Match,
  getCs2Play,
  getCs2PlayerProfile,
  getCs2Recap,
  getCs2Season,
  getCs2Squad,
  getCs2Training,
  getCs2Utility,
  listCs2Highlights,
  listCs2Matches,
  retryCs2Match,
  saveCs2TrainingResult,
  toggleCs2Reaction,
  updateCs2Play,
  updateCs2Role,
  uploadCs2Match,
  type Cs2Availability,
  type Cs2Highlight,
  type Cs2Leaderboards,
  type Cs2MatchSummary,
  type Cs2TrainingKind,
} from '../api/counterStrike'
import { listOrganizations } from '../api/organizations'
import { ApiError } from '../api/client'
import { cs2Path } from './counterStrikeRoutes'

const navItems = [
  { to: '', label: 'Home', icon: Home, end: true },
  { to: 'play', label: 'Play', icon: Gamepad2 },
  { to: 'matches', label: 'Matches', icon: Swords },
  { to: 'season', label: 'Season', icon: Trophy },
  { to: 'highlights', label: 'Highlights', icon: Flame },
  { to: 'training', label: 'Training', icon: Crosshair },
  { to: 'squad', label: 'Squad', icon: Users },
  { to: 'stats', label: 'Stats', icon: BarChart3 },
] as const

export function CounterStrikeEntry() {
  const organizations = useQuery({
    queryKey: ['organizations'],
    queryFn: listOrganizations,
  })
  if (organizations.isPending) return <Cs2BootScreen />
  if (!organizations.data?.length) {
    return (
      <div className="cs2-standalone-message">
        <Crosshair size={42} />
        <h1>Erst eine Community anlegen</h1>
        <p>Der CS2-Bereich nutzt deine bestehende CouchClash-Community.</p>
        <Link to="/organizations/new" className="cs2-button cs2-button--primary">
          Community erstellen
        </Link>
      </div>
    )
  }
  const remembered = localStorage.getItem('couchclash-cs2-organization')
  const organization =
    organizations.data.find((item) => item.id === remembered) ??
    organizations.data[0]
  return <Navigate to={`/cs2/${organization.id}`} replace />
}

export function CounterStrikeApp({ user }: { user: CurrentUser }) {
  const { organizationId = '' } = useParams()
  const [mobileOpen, setMobileOpen] = useState(false)
  const organizations = useQuery({
    queryKey: ['organizations'],
    queryFn: listOrganizations,
  })
  const current = organizations.data?.find((item) => item.id === organizationId)

  useEffect(() => {
    if (organizationId) {
      localStorage.setItem('couchclash-cs2-organization', organizationId)
    }
  }, [organizationId])

  return (
    <div className="cs2-root">
      <div className="cs2-atmosphere" />
      <header className="cs2-topbar">
        <Link to={`/cs2/${organizationId}`} className="cs2-brand">
          <span className="cs2-brand__mark"><Crosshair size={22} /></span>
          <span><b>COUCH</b>CLASH <em>CS2</em></span>
        </Link>
        <div className="cs2-topbar__right">
          {organizations.data && organizations.data.length > 1 && (
            <select
              aria-label="Community wechseln"
              value={organizationId}
              onChange={(event) => {
                window.location.href = `/cs2/${event.target.value}`
              }}
            >
              {organizations.data.map((organization) => (
                <option key={organization.id} value={organization.id}>
                  {organization.name}
                </option>
              ))}
            </select>
          )}
          <Link to="/account" className="cs2-user-chip">
            <Avatar name={user.displayName} url={user.avatarUrl} />
            <span>{user.displayName}</span>
          </Link>
          <button
            className="cs2-mobile-menu"
            onClick={() => setMobileOpen((value) => !value)}
            aria-label="Navigation öffnen"
          >
            {mobileOpen ? <X /> : <Menu />}
          </button>
        </div>
      </header>
      <div className="cs2-layout">
        <aside className={`cs2-sidebar ${mobileOpen ? 'is-open' : ''}`}>
          <div className="cs2-community-label">
            <span>ACTIVE SQUAD</span>
            <strong>{current?.name ?? 'CouchClash'}</strong>
          </div>
          <nav>
            {navItems.map((item) => {
              const Icon = item.icon
              return (
                <NavLink
                  key={item.to}
                  to={cs2Path(organizationId, item.to)}
                  end={'end' in item ? item.end : false}
                  onClick={() => setMobileOpen(false)}
                >
                  <Icon size={19} />
                  <span>{item.label}</span>
                </NavLink>
              )
            })}
          </nav>
          <div className="cs2-sidebar__footer">
            <Shield size={16} /> Community-gebunden
            <Link to={`/organizations/${organizationId}`}>Zum Intranet</Link>
          </div>
        </aside>
        <main className="cs2-main">
          <Routes>
            <Route index element={<Cs2Home />} />
            <Route path="play" element={<Cs2Play />} />
            <Route path="matches" element={<Cs2Matches />} />
            <Route path="matches/:matchId" element={<Cs2MatchDetail />} />
            <Route path="season" element={<Cs2Season canManage={current?.permissionRole === 'Owner' || current?.permissionRole === 'Administrator'} />} />
            <Route path="season/recap" element={<Cs2Recap />} />
            <Route path="highlights" element={<Cs2Highlights />} />
            <Route path="training" element={<Cs2Training />} />
            <Route path="training/aim" element={<Cs2AimTrainer />} />
            <Route path="training/utility" element={<Cs2UtilityTraining />} />
            <Route path="squad" element={<Cs2Squad user={user} />} />
            <Route path="squad/:userId" element={<Cs2PlayerProfile />} />
            <Route path="stats" element={<Cs2Stats />} />
            <Route path="*" element={<Navigate to={cs2Path(organizationId)} replace />} />
          </Routes>
        </main>
      </div>
    </div>
  )
}

function Cs2Home() {
  const organizationId = useOrganizationId()
  const dashboard = useQuery({
    queryKey: ['cs2-dashboard', organizationId],
    queryFn: () => getCs2Dashboard(organizationId),
    refetchInterval: 30_000,
  })
  if (dashboard.isPending) return <Cs2Loading label="TACTICAL DATA LOADING" />
  if (dashboard.error) return <Cs2Error error={dashboard.error} />
  const data = dashboard.data
  return (
    <>
      <PageHeader
        eyebrow={data.season.name}
        title="Squad Overview"
        text="Eure Matches, eure Momente, euer nächster Schritt."
      />
      <section className="cs2-hero-grid">
        <div className="cs2-scoreboard-card">
          <div className="cs2-scoreboard-card__label">SEASON RECORD</div>
          <div className="cs2-record">
            <span className="wins">{data.summary.wins}W</span>
            <i>—</i>
            <span className="losses">{data.summary.losses}L</span>
          </div>
          <div className="cs2-winrate">
            <span style={{ width: `${Math.min(100, data.summary.winRate)}%` }} />
          </div>
          <div className="cs2-scoreboard-meta">
            <span>{data.summary.matches} Matches</span>
            <strong>{data.summary.winRate.toFixed(0)}% Winrate</strong>
            <span className={data.summary.streakType === 'W' ? 'positive' : 'negative'}>
              {data.summary.streak}{data.summary.streakType} Streak
            </span>
          </div>
        </div>
        <Link to={cs2Path(organizationId, 'play')} className={`cs2-stack-card ${data.play.fullStack ? 'is-ready' : ''}`}>
          <div className="cs2-stack-card__pulse" />
          <span>HEUTE CS?</span>
          <strong>
            {data.play.fullStack
              ? data.play.substitutes > 0
                ? `${data.play.yes} READY · +${data.play.substitutes} BACKUP`
                : 'FULL STACK READY'
              : `${data.play.yes}/5 READY`}
          </strong>
          <p>
            {data.play.fullStack
              ? 'Alle weiteren Zusagen bleiben als Backup dabei.'
              : `Noch ${data.play.missing} bis zum vollen Stack.`}
          </p>
          <ChevronRight />
        </Link>
      </section>

      <div className="cs2-section-title">
        <div><span>LAST DEPLOYMENT</span><h2>Letztes Match</h2></div>
        <Link to={cs2Path(organizationId, 'matches')}>Alle Matches <ChevronRight size={16} /></Link>
      </div>
      {data.lastMatch ? <MatchCard match={data.lastMatch} featured /> : <EmptyCard text="Noch keine Demo importiert." />}

      <section className="cs2-dashboard-columns">
        <div>
          <div className="cs2-section-title compact"><div><span>TOP PLAYS</span><h2>Neue Highlights</h2></div></div>
          <div className="cs2-highlight-list">
            {data.highlights.length ? data.highlights.map((highlight) => (
              <HighlightCard key={highlight.id} highlight={highlight} compact />
            )) : <EmptyCard text="Die Highlight Engine wartet auf eure erste Demo." />}
          </div>
        </div>
        <div>
          <div className="cs2-section-title compact"><div><span>PERSONAL MISSION</span><h2>Nächstes Training</h2></div></div>
          <Link to={cs2Path(organizationId, `training/${data.recommendation.route}`)} className="cs2-training-callout">
            <Target size={30} />
            <span>{data.recommendation.kind}</span>
            <h3>{data.recommendation.title}</h3>
            <p>{data.recommendation.reason}</p>
            <b>TRAINING STARTEN <ChevronRight size={15} /></b>
          </Link>
          <div className="cs2-award-row">
            {data.awards.map((award) => (
              <div key={award.id} className="cs2-mini-award">
                <span>{award.icon}</span>
                <div><b>{award.name}</b><small>{award.displayName}</small></div>
              </div>
            ))}
          </div>
        </div>
      </section>
    </>
  )
}

function Cs2Play() {
  const organizationId = useOrganizationId()
  const queryClient = useQueryClient()
  const play = useQuery({
    queryKey: ['cs2-play', organizationId],
    queryFn: () => getCs2Play(organizationId),
    refetchInterval: 15_000,
  })
  const mutation = useMutation({
    mutationFn: (availability: Cs2Availability) =>
      updateCs2Play(organizationId, { availability }),
    onSuccess: (data) => {
      queryClient.setQueryData(['cs2-play', organizationId], data)
      void queryClient.invalidateQueries({ queryKey: ['cs2-dashboard', organizationId] })
    },
  })
  if (play.isPending) return <Cs2Loading label="CHECKING THE LOBBY" />
  if (play.error) return <Cs2Error error={play.error} />
  return (
    <>
      <PageHeader eyebrow="QUICK SESSION" title="Heute CS?" text="Ein Klick. Kein Kalender. Kein Meeting-Overhead." />
      <section className={`cs2-ready-stage ${play.data.fullStack ? 'is-full' : ''}`}>
        <div className="cs2-ready-ring">
          <span>{play.data.yes}</span><small>/ 5+</small>
        </div>
        <div>
          <span className="cs2-kicker">SQUAD STATUS</span>
          <h2>
            {play.data.fullStack
              ? play.data.substitutes > 0
                ? `FULL STACK + ${play.data.substitutes} BACKUP${play.data.substitutes === 1 ? '' : 'S'}`
                : 'FULL STACK READY'
              : `${play.data.missing} SPIELER FEHLEN`}
          </h2>
          <p>{play.data.yes} feste Zusagen · {play.data.maybe} vielleicht dabei · Start {play.data.plannedStart?.slice(0, 5) ?? 'offen'}</p>
        </div>
      </section>
      <div className="cs2-availability-buttons">
        {([
          ['Yes', 'Dabei', 'Lock it in'],
          ['Maybe', 'Vielleicht', 'Noch unsicher'],
          ['No', 'Nicht dabei', 'Heute Pause'],
        ] as const).map(([value, label, hint]) => (
          <button
            key={value}
            className={play.data.mine === value ? 'is-active' : ''}
            onClick={() => mutation.mutate(value)}
            disabled={mutation.isPending}
          >
            <span>{value === 'Yes' ? '✓' : value === 'Maybe' ? '?' : '×'}</span>
            <b>{label}</b><small>{hint}</small>
          </button>
        ))}
      </div>
      <section className="cs2-panel">
        <div className="cs2-section-title compact"><div><span>LOBBY</span><h2>Wer ist am Start?</h2></div></div>
        <div className="cs2-player-ready-list">
          {play.data.participants.map((participant) => (
            <div key={participant.userId}>
              <Avatar name={participant.displayName} url={participant.avatarUrl} />
              <div><b>{participant.displayName}</b><small>{participant.availableFrom ? `ab ${participant.availableFrom.slice(0, 5)}` : 'zeitlich flexibel'}</small></div>
              <StatusPill status={participant.availability} />
            </div>
          ))}
          {!play.data.participants.length && <p className="cs2-muted">Sei der Erste, der sich einträgt.</p>}
        </div>
      </section>
    </>
  )
}

function Cs2Matches() {
  const organizationId = useOrganizationId()
  const queryClient = useQueryClient()
  const [file, setFile] = useState<File>()
  const matches = useQuery({
    queryKey: ['cs2-matches', organizationId],
    queryFn: () => listCs2Matches(organizationId),
    refetchInterval: (query) =>
      query.state.data?.some((item) => item.status === 'Uploaded' || item.status === 'Processing') ? 3_000 : false,
  })
  const upload = useMutation({
    mutationFn: (demo: File) => uploadCs2Match(organizationId, demo),
    onSuccess: () => {
      setFile(undefined)
      void queryClient.invalidateQueries({ queryKey: ['cs2-matches', organizationId] })
    },
  })
  const retry = useMutation({
    mutationFn: (matchId: string) => retryCs2Match(organizationId, matchId),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['cs2-matches', organizationId] }),
  })
  const chooseFile = (event: ChangeEvent<HTMLInputElement>) => setFile(event.target.files?.[0])
  return (
    <>
      <PageHeader eyebrow="MATCH ARCHIVE" title="Matches" text="Demo rein. Story, Stats und Highlights raus." />
      <section className="cs2-upload-card">
        <div className="cs2-upload-icon"><Upload /></div>
        <div><span>CS2 DEMO IMPORT</span><h2>{file?.name ?? '.dem hier auswählen'}</h2><p>Asynchron, per SHA-256 dedupliziert und außerhalb des Webroots verarbeitet.</p></div>
        <label className="cs2-button cs2-button--ghost">
          Datei wählen<input type="file" accept=".dem,application/octet-stream" onChange={chooseFile} hidden />
        </label>
        <button className="cs2-button cs2-button--primary" disabled={!file || upload.isPending} onClick={() => file && upload.mutate(file)}>
          {upload.isPending ? 'UPLOAD …' : 'ANALYSE STARTEN'}
        </button>
      </section>
      {upload.error && <Cs2Error error={upload.error} />}
      <div className="cs2-match-grid">
        {matches.data?.map((match) => (
          <div key={match.id}>
            <MatchCard match={match} />
            {match.status === 'Failed' && (
              <button className="cs2-inline-retry" onClick={() => retry.mutate(match.id)}><RefreshCw size={15} /> Erneut analysieren</button>
            )}
          </div>
        ))}
      </div>
      {matches.isPending && <Cs2Loading label="LOADING MATCHES" />}
      {matches.error && <Cs2Error error={matches.error} />}
    </>
  )
}

function Cs2MatchDetail() {
  const organizationId = useOrganizationId()
  const { matchId = '' } = useParams()
  const match = useQuery({
    queryKey: ['cs2-match', organizationId, matchId],
    queryFn: () => getCs2Match(organizationId, matchId),
    refetchInterval: (query) => ['Uploaded', 'Processing'].includes(query.state.data?.match.status ?? '') ? 3_000 : false,
  })
  if (match.isPending) return <Cs2Loading label="RECONSTRUCTING MATCH" />
  if (match.error) return <Cs2Error error={match.error} />
  const data = match.data
  if (data.match.status !== 'Completed') {
    return <ProcessingMatch match={data.match} />
  }
  return (
    <>
      <Link to={cs2Path(organizationId, 'matches')} className="cs2-back-link">← Matches</Link>
      <section className={`cs2-match-hero ${data.match.win ? 'is-win' : 'is-loss'}`}>
        <div><span>{data.match.win ? 'VICTORY' : 'DEFEAT'}</span><h1>{data.match.mapName}</h1><p>{formatDate(data.match.playedAt)}</p></div>
        <div className="cs2-match-hero__score"><b>{data.match.teamAScore}</b><i>:</i><b>{data.match.teamBScore}</b></div>
        <div className="cs2-match-hero__teams"><span>{data.match.teamAName}</span><span>{data.match.teamBName}</span></div>
      </section>
      {!!data.story.length && (
        <section className="cs2-story-strip">
          <Sparkles />
          <div><span>MATCH STORY</span>{data.story.map((line) => <b key={line}>{line}</b>)}</div>
        </section>
      )}
      <div className="cs2-section-title"><div><span>PLAYER DATA</span><h2>Scoreboard</h2></div></div>
      <div className="cs2-table-wrap">
        <table className="cs2-table">
          <thead><tr><th>Spieler</th><th>K</th><th>D</th><th>A</th><th>K/D</th><th>ADR</th><th>KAST</th><th>HS%</th><th>UD</th><th>FK</th><th>TRD</th><th>Rating</th></tr></thead>
          <tbody>{data.players.map((player) => (
            <tr key={player.id}>
              <td><Avatar name={player.displayName} /><b>{player.displayName}</b><small>{player.teamName}</small></td>
              <td>{player.kills}</td><td>{player.deaths}</td><td>{player.assists}</td>
              <td>{(player.kills / Math.max(1, player.deaths)).toFixed(2)}</td><td>{player.adr.toFixed(0)}</td>
              <td>{player.kast.toFixed(0)}%</td><td>{player.headshotPercent.toFixed(0)}%</td><td>{player.utilityDamage}</td>
              <td>{player.firstKills - player.firstDeaths >= 0 ? '+' : ''}{player.firstKills - player.firstDeaths}</td>
              <td>{player.tradeKills}</td><td><strong>{player.hltvRating.toFixed(2)}</strong></td>
            </tr>
          ))}</tbody>
        </table>
      </div>
      <div className="cs2-section-title"><div><span>AUTO-DETECTED</span><h2>Highlights</h2></div></div>
      <div className="cs2-card-grid">{data.highlights.map((highlight) => <HighlightCard key={highlight.id} highlight={highlight} />)}</div>
    </>
  )
}

function Cs2Season({ canManage }: { canManage: boolean }) {
  const organizationId = useOrganizationId()
  const queryClient = useQueryClient()
  const [seasonName, setSeasonName] = useState('')
  const season = useQuery({ queryKey: ['cs2-season', organizationId], queryFn: () => getCs2Season(organizationId) })
  const leaders = useQuery({ queryKey: ['cs2-leaders', organizationId], queryFn: () => getCs2Leaderboards(organizationId) })
  const create = useMutation({ mutationFn: () => createCs2Season(organizationId, seasonName), onSuccess: () => {
    setSeasonName(''); void queryClient.invalidateQueries({ queryKey: ['cs2-season', organizationId] }); void queryClient.invalidateQueries({ queryKey: ['cs2-leaders', organizationId] })
  } })
  const close = useMutation({ mutationFn: (seasonId: string) => closeCs2Season(organizationId, seasonId), onSuccess: () => {
    void queryClient.invalidateQueries({ queryKey: ['cs2-season', organizationId] }); void queryClient.invalidateQueries({ queryKey: ['cs2-leaders', organizationId] })
  } })
  if (season.isPending || leaders.isPending) return <Cs2Loading label="CALCULATING SEASON" />
  if (season.error || leaders.error) return <Cs2Error error={season.error ?? leaders.error} />
  return (
    <>
      <PageHeader eyebrow="ACTIVE SEASON" title={season.data.name} text={`${formatDate(season.data.startsAt)} – ${season.data.endsAt ? formatDate(season.data.endsAt) : 'läuft'}`} />
      <div className="cs2-stat-row">
        <StatTile label="Matches" value={season.data.matches} />
        <StatTile label="Wins" value={season.data.wins} tone="green" />
        <StatTile label="Losses" value={season.data.losses} tone="red" />
        <StatTile label="Winrate" value={`${season.data.winRate.toFixed(0)}%`} />
      </div>
      <Link to={cs2Path(organizationId, 'season/recap')} className="cs2-recap-link"><Sparkles /> Season Recap öffnen <ChevronRight /></Link>
      {canManage && <section className="cs2-season-admin"><div><span>ADMIN</span><b>Season verwalten</b></div><input value={seasonName} onChange={(event) => setSeasonName(event.target.value)} maxLength={120} placeholder="Neue Season" /><button disabled={!seasonName.trim() || create.isPending} onClick={() => create.mutate()}>Neu anlegen</button><button className="danger" disabled={close.isPending} onClick={() => close.mutate(season.data.id)}>Aktuelle abschließen</button></section>}
      <LeaderboardSections data={leaders.data} />
    </>
  )
}

function Cs2Recap() {
  const organizationId = useOrganizationId()
  const recap = useQuery({ queryKey: ['cs2-recap', organizationId], queryFn: () => getCs2Recap(organizationId) })
  if (recap.isPending) return <Cs2Loading label="CUTTING THE RECAP" />
  if (recap.error) return <Cs2Error error={recap.error} />
  const data = recap.data
  return (
    <>
      <Link to={cs2Path(organizationId, 'season')} className="cs2-back-link">← Season</Link>
      <PageHeader eyebrow="SEASON RECAP" title={data.season.name} text="Die Saison als kompakte Story – bereit für spätere Video-Recaps." />
      <section className="cs2-recap-hero">
        <Sparkles />
        <div><span>FINAL RECORD</span><h2>{data.summary.wins}W — {data.summary.losses}L</h2><p>{data.summary.matches} Matches · {data.summary.winRate.toFixed(0)}% Winrate</p></div>
      </section>
      <div className="cs2-recap-maps">
        <div><span>BEST MAP</span><b>{data.bestMap?.map ?? '–'}</b><small>{data.bestMap ? `${data.bestMap.winRate.toFixed(0)}% Winrate` : 'Noch keine Daten'}</small></div>
        <div><span>TOUGHEST MAP</span><b>{data.worstMap?.map ?? '–'}</b><small>{data.worstMap ? `${data.worstMap.winRate.toFixed(0)}% Winrate` : 'Noch keine Daten'}</small></div>
        <div><span>ACES</span><b>{data.aces}</b><small>{data.winStreak}W längster Streak</small></div>
        <div><span>CLUTCHES</span><b>{data.clutches}</b><small>1vX gewonnen</small></div>
      </div>
      <div className="cs2-section-title"><div><span>THE HARDWARE</span><h2>Awards</h2></div></div>
      <div className="cs2-awards-grid">{data.awards.map((award) => <article key={award.id}><span>{award.icon}</span><h3>{award.name}</h3><b>{award.displayName}</b><p>{award.description}</p></article>)}</div>
      <div className="cs2-section-title"><div><span>TOP PLAYS</span><h2>Highlights der Season</h2></div></div>
      <div className="cs2-card-grid">{data.highlights.map((highlight) => <HighlightCard key={highlight.id} highlight={highlight} />)}</div>
    </>
  )
}

function Cs2Stats() {
  const organizationId = useOrganizationId()
  const leaders = useQuery({ queryKey: ['cs2-leaders', organizationId], queryFn: () => getCs2Leaderboards(organizationId) })
  if (leaders.isPending) return <Cs2Loading label="CRUNCHING NUMBERS" />
  if (leaders.error) return <Cs2Error error={leaders.error} />
  return <><PageHeader eyebrow="SEASON ANALYTICS" title="Stats" text="Mehrere Blickwinkel statt einer nutzlosen Gesamtrangliste." /><LeaderboardSections data={leaders.data} /></>
}

function LeaderboardSections({ data }: { data: Cs2Leaderboards }) {
  return (
    <div className="cs2-leaderboard-grid">
      <Leaderboard title="Performance" icon={<Activity />} players={data.performance} metric={(player) => player.hltvRating.toFixed(2)} detail={(player) => `${player.adr.toFixed(0)} ADR · ${player.kd.toFixed(2)} K/D`} />
      <Leaderboard title="Impact" icon={<Zap />} players={data.impact} metric={(player) => `${player.entryDifference >= 0 ? '+' : ''}${player.entryDifference}`} detail={(player) => `${player.tradeKills} Trades · ${player.utilityDamage} UD`} />
      <Leaderboard title="Clutch" icon={<Shield />} players={data.clutch} metric={(player) => String(player.clutchesWon)} detail={() => 'gewonnene 1vX'} />
      <Leaderboard title="Multi Kills" icon={<Flame />} players={data.multiKills} metric={(player) => String(player.aces)} detail={(player) => `${player.fourKills} 4Ks · ${player.threeKills} 3Ks`} />
    </div>
  )
}

function Cs2Highlights() {
  const organizationId = useOrganizationId()
  const queryClient = useQueryClient()
  const highlights = useQuery({ queryKey: ['cs2-highlights', organizationId], queryFn: () => listCs2Highlights(organizationId) })
  const reaction = useMutation({
    mutationFn: ({ id, value }: { id: string; value: string }) => toggleCs2Reaction(organizationId, id, value),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['cs2-highlights', organizationId] }),
  })
  return (
    <>
      <PageHeader eyebrow="COMMUNITY MOMENTS" title="Highlights" text="Aces, Clutches und der Quatsch, über den ihr noch Wochen redet." />
      {highlights.isPending && <Cs2Loading label="FINDING THE GOOD STUFF" />}
      {highlights.error && <Cs2Error error={highlights.error} />}
      <div className="cs2-card-grid">{highlights.data?.map((highlight) => (
        <HighlightCard key={highlight.id} highlight={highlight} onReact={(value) => reaction.mutate({ id: highlight.id, value })} />
      ))}</div>
    </>
  )
}

function Cs2Training() {
  const organizationId = useOrganizationId()
  const training = useQuery({ queryKey: ['cs2-training', organizationId], queryFn: () => getCs2Training(organizationId) })
  const challenges = useQuery({ queryKey: ['cs2-challenges', organizationId], queryFn: () => getCs2Challenges(organizationId) })
  if (training.isPending) return <Cs2Loading label="BUILDING YOUR PLAN" />
  if (training.error) return <Cs2Error error={training.error} />
  return (
    <>
      <PageHeader eyebrow="GET BETTER TOGETHER" title="Training" text="Demo → Schwäche → kurze Session → im nächsten Match prüfen." />
      <section className="cs2-plan-card">
        <div className="cs2-plan-card__head"><div><span>HEUTE</span><h2>{training.data.plan.plannedMinutes} Minuten</h2><p>{training.data.plan.recommendationReason}</p></div><Crosshair size={42} /></div>
        <div className="cs2-plan-steps">{training.data.exercises.map((exercise, index) => (
          <Link key={exercise.id} to={cs2Path(organizationId, exercise.kind === 'Utility' ? 'training/utility' : `training/aim?mode=${aimModeFor(exercise.kind)}`)}>
            <span>{String(index + 1).padStart(2, '0')}</span><div><b>{exercise.name}</b><small>{exercise.durationMinutes} MIN · {exercise.kind}</small></div><ChevronRight />
          </Link>
        ))}</div>
      </section>
      <div className="cs2-training-modes">
        <Link to={cs2Path(organizationId, 'training/aim?mode=flick')}><Target /><b>Flick</b><small>Präzision & Tempo</small></Link>
        <Link to={cs2Path(organizationId, 'training/aim?mode=reaction')}><Zap /><b>Reaction</b><small>Erster Kontakt</small></Link>
        <Link to={cs2Path(organizationId, 'training/aim?mode=switching')}><Crosshair /><b>Switching</b><small>Mehrere Targets</small></Link>
        <Link to={cs2Path(organizationId, 'training/aim?mode=tracking')}><Activity /><b>Tracking</b><small>Am Target bleiben</small></Link>
        <Link to={cs2Path(organizationId, 'training/utility')}><Shield /><b>Utility</b><small>Map Line-ups</small></Link>
      </div>
      <div className="cs2-section-title"><div><span>SQUAD GOAL</span><h2>Weekly Challenge</h2></div></div>
      {challenges.data?.map((challenge) => {
        const value = challenge.mine?.value ?? 0
        return <div key={challenge.id} className="cs2-challenge-card"><Award /><div><b>{challenge.name}</b><p>{challenge.description}</p><div className="cs2-progress"><span style={{ width: `${Math.min(100, value / challenge.targetValue * 100)}%` }} /></div><small>{value.toFixed(1)} / {challenge.targetValue} · {challenge.squad.length} im Squad aktiv</small></div></div>
      })}
      {!!training.data.history.length && <><div className="cs2-section-title"><div><span>PROGRESS</span><h2>Letzte Sessions</h2></div></div><div className="cs2-history-row">{training.data.history.slice(0, 6).map((result) => <div key={result.id}><span>{result.kind}</span><b>{result.accuracy.toFixed(0)}%</b><small>{formatDate(result.completedAt)}</small></div>)}</div></>}
    </>
  )
}

function Cs2UtilityTraining() {
  const organizationId = useOrganizationId()
  const drills = useQuery({ queryKey: ['cs2-utility', organizationId], queryFn: () => getCs2Utility(organizationId) })
  const [done, setDone] = useState<Record<string, number>>({})
  const save = useMutation({ mutationFn: (trainingExerciseId: string) => saveCs2TrainingResult(organizationId, {
    kind: 'Utility', durationSeconds: 180, hits: 3, misses: 0, reactionTimeMs: 0,
    flickTimeMs: 0, trackingPercent: 0, repetitions: 3, trainingExerciseId,
  }) })
  const completeRepetition = (drillId: string) => {
    const next = Math.min(3, (done[drillId] ?? 0) + 1)
    setDone((value) => ({ ...value, [drillId]: next }))
    if (next === 3 && (done[drillId] ?? 0) < 3) save.mutate(drillId)
  }
  return (
    <>
      <Link to={cs2Path(organizationId, 'training')} className="cs2-back-link">← Training</Link>
      <PageHeader eyebrow="UTILITY LAB" title="Mirage Set" text="Keine 3D-Spielerei: klare Line-ups, Wiederholungen und Match-Transfer." />
      {drills.isPending && <Cs2Loading label="PREPARING NADES" />}
      <div className="cs2-utility-grid">{drills.data?.map((drill) => (
        <div key={drill.id} className={done[drill.id] ? 'is-done' : ''}>
          <div className="cs2-map-placeholder"><span>{drill.mapName}</span><Crosshair /></div>
          <span>{drill.position} → {drill.target}</span><h3>{drill.name}</h3><p>{drill.description}</p>
          <button onClick={() => completeRepetition(drill.id)} disabled={(done[drill.id] ?? 0) >= 3 || save.isPending}><Check /> Wiederholung {done[drill.id] ?? 0}/3</button>
        </div>
      ))}</div>
    </>
  )
}

function Cs2Squad({ user }: { user: CurrentUser }) {
  const organizationId = useOrganizationId()
  const queryClient = useQueryClient()
  const squad = useQuery({ queryKey: ['cs2-squad', organizationId], queryFn: () => getCs2Squad(organizationId) })
  const role = useMutation({
    mutationFn: (value: string) => updateCs2Role(organizationId, value),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['cs2-squad', organizationId] }),
  })
  return (
    <>
      <PageHeader eyebrow="YOUR ROSTER" title="Squad" text="Alle Gruppenmitglieder, persönliche Bilanzen und euer gemeinsamer 5-Stack-Record." />
      {squad.isPending && <Cs2Loading label="ASSEMBLING SQUAD" />}
      {squad.error && <Cs2Error error={squad.error} />}
      {squad.data && <section className="cs2-squad-records">
        <SquadRecordCard
          eyebrow="ALLE SPIELERGEBNISSE"
          title="Kumulierte Bilanz"
          record={squad.data.summary.playerRecord}
          text="Siege und Niederlagen aller persönlichen Match-Teilnahmen."
        />
        <SquadRecordCard
          eyebrow="ECHTER 5-STACK"
          title="Team-Bilanz"
          record={squad.data.summary.fullSquadRecord}
          text="Nur Matches mit fünf erkannten Mitgliedern eurer Gruppe."
          featured
        />
      </section>}
      <div className="cs2-squad-grid">{squad.data?.players.map((player) => (
        <article key={player.id}>
          <div className="cs2-squad-avatar"><Avatar name={player.displayName} url={player.steamAvatarUrl ?? player.avatarUrl} /><span className={player.steamId64 ? 'online' : ''} /></div>
          <span>{player.role === 'Unset' ? 'NO ROLE' : player.role.toUpperCase()}</span><h2><Link to={cs2Path(organizationId, `squad/${player.id}`)}>{player.steamName ?? player.displayName}</Link></h2><p>{player.steamId64 ? `Steam ${player.steamId64.slice(-6)}` : 'Steam noch nicht verbunden'}</p>
          {player.stats ? <div className="cs2-player-numbers"><div className="wins"><b>{player.stats.wins}W</b><small>WINS</small></div><div className="losses"><b>{player.stats.losses}L</b><small>LOSSES</small></div><div><b>{player.stats.hltvRating.toFixed(2)}</b><small>RATING</small></div><div><b>{player.stats.kd.toFixed(2)}</b><small>K/D</small></div><div><b>{player.stats.adr.toFixed(0)}</b><small>ADR</small></div></div> : <div className="cs2-no-stats">Noch kein Match zugeordnet</div>}
          {player.id === user.id && <select value={player.role} onChange={(event) => role.mutate(event.target.value)}>{['Unset', 'Igl', 'Entry', 'Rifler', 'Awper', 'Support', 'Lurker'].map((value) => <option key={value} value={value}>{value}</option>)}</select>}
        </article>
      ))}</div>
    </>
  )
}

function Cs2PlayerProfile() {
  const organizationId = useOrganizationId()
  const { userId = '' } = useParams()
  const profile = useQuery({ queryKey: ['cs2-player', organizationId, userId], queryFn: () => getCs2PlayerProfile(organizationId, userId) })
  if (profile.isPending) return <Cs2Loading label="LOADING PLAYER PROFILE" />
  if (profile.error) return <Cs2Error error={profile.error} />
  const data = profile.data
  const stats = data.stats
  return (
    <>
      <Link to={cs2Path(organizationId, 'squad')} className="cs2-back-link">← Squad</Link>
      <section className="cs2-profile-hero">
        <Avatar name={data.player.displayName} url={data.steam?.avatarUrl ?? data.player.avatarUrl} />
        <div><span>{data.role.toUpperCase()} · {data.favoriteMap ?? 'NO FAVORITE MAP'}</span><h1>{data.steam?.displayName ?? data.player.displayName}</h1><p>{data.steam ? `Steam ${data.steam.steamId64}` : 'Steam nicht verbunden'}</p></div>
      </section>
      <div className="cs2-stat-row">
        <StatTile label="Matches" value={stats?.matches ?? 0} />
        <StatTile label="K/D" value={stats ? (stats.kills / Math.max(1, stats.deaths)).toFixed(2) : '–'} />
        <StatTile label="ADR" value={stats?.adr.toFixed(0) ?? '–'} />
        <StatTile label="Rating" value={stats?.hltvRating.toFixed(2) ?? '–'} tone="green" />
      </div>
      <section className="cs2-trend-panel">
        <div><span>FORM CHECK</span><h2>Letzte 5 vs. letzte 20</h2></div>
        <div className="cs2-table-wrap"><table className="cs2-table"><thead><tr><th>Zeitraum</th><th>Matches</th><th>K/D</th><th>ADR</th><th>KAST</th><th>Rating</th></tr></thead><tbody>{([['Letzte 5', data.trends.last5], ['Letzte 20', data.trends.last20]] as const).map(([label, trend]) => <tr key={label}><td><b>{label}</b></td><td>{trend.matches}</td><td>{trend.kd.toFixed(2)}</td><td>{trend.adr.toFixed(0)}</td><td>{trend.kast.toFixed(0)}%</td><td><strong>{trend.rating.toFixed(2)}</strong></td></tr>)}</tbody></table></div>
      </section>
      <div className="cs2-profile-columns">
        <section><div className="cs2-section-title compact"><div><span>HONORS</span><h2>Awards</h2></div></div><div className="cs2-awards-grid">{data.awards.map((award) => <article key={award.id}><span>{award.icon}</span><h3>{award.name}</h3><p>{award.description}</p></article>)}</div></section>
        <section><div className="cs2-section-title compact"><div><span>PROGRESS</span><h2>Training</h2></div></div><div className="cs2-training-summary"><Crosshair /><b>{data.training.sessions}</b><span>Sessions</span><small>{data.training.averageAccuracy.toFixed(0)}% Ø Accuracy</small></div></section>
      </div>
      <div className="cs2-section-title"><div><span>TOP PLAYS</span><h2>Highlights</h2></div></div>
      <div className="cs2-card-grid">{data.highlights.map((highlight) => <HighlightCard key={highlight.id} highlight={highlight} />)}</div>
    </>
  )
}

type AimMode = 'flick' | 'reaction' | 'switching' | 'tracking'
interface AimMetrics { hits: number; misses: number; reactionTotal: number; reactions: number; trackingMs: number; startedAt: number }
interface AimTarget { x: number; y: number; radius: number; bornAt: number; vx: number; vy: number }

function Cs2AimTrainer() {
  const organizationId = useOrganizationId()
  const [params, setParams] = useSearchParams()
  const mode = (['flick', 'reaction', 'switching', 'tracking'].includes(params.get('mode') ?? '') ? params.get('mode') : 'flick') as AimMode
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const frameRef = useRef(0)
  const runningRef = useRef(false)
  const targetRef = useRef<AimTarget | null>(null)
  const cursorRef = useRef({ x: 0, y: 0 })
  const nextTargetAtRef = useRef(0)
  const metricsRef = useRef<AimMetrics>({ hits: 0, misses: 0, reactionTotal: 0, reactions: 0, trackingMs: 0, startedAt: 0 })
  const [running, setRunning] = useState(false)
  const [timeLeft, setTimeLeft] = useState(20)
  const [result, setResult] = useState<{ hits: number; misses: number; accuracy: number; reaction: number; tracking: number }>()
  const save = useMutation({ mutationFn: (input: Parameters<typeof saveCs2TrainingResult>[1]) => saveCs2TrainingResult(organizationId, input) })

  const spawnTarget = useCallback((canvas: HTMLCanvasElement, now: number) => {
    const radius = mode === 'tracking' ? 24 : 20
    targetRef.current = {
      x: radius + Math.random() * (canvas.width - radius * 2),
      y: radius + Math.random() * (canvas.height - radius * 2),
      radius,
      bornAt: now,
      vx: (Math.random() > 0.5 ? 1 : -1) * (mode === 'tracking' ? 120 : 0),
      vy: (Math.random() - 0.5) * (mode === 'tracking' ? 100 : 0),
    }
  }, [mode])

  const finish = useCallback(() => {
    runningRef.current = false
    setRunning(false)
    document.exitPointerLock?.()
    const metrics = metricsRef.current
    const attempts = metrics.hits + metrics.misses
    const summary = {
      hits: metrics.hits,
      misses: metrics.misses,
      accuracy: attempts ? metrics.hits * 100 / attempts : 0,
      reaction: metrics.reactions ? metrics.reactionTotal / metrics.reactions : 0,
      tracking: Math.min(100, metrics.trackingMs / 20_000 * 100),
    }
    setResult(summary)
    save.mutate({
      kind: aimKind(mode), durationSeconds: 20, hits: summary.hits, misses: summary.misses,
      reactionTimeMs: summary.reaction, flickTimeMs: mode === 'flick' ? summary.reaction : 0,
      trackingPercent: summary.tracking, repetitions: summary.hits,
    })
  }, [mode, save])

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    const context = canvas.getContext('2d')
    if (!context) return
    let previous = performance.now()
    const draw = (now: number) => {
      const rect = canvas.getBoundingClientRect()
      if (canvas.width !== Math.round(rect.width) || canvas.height !== Math.round(rect.height)) {
        canvas.width = Math.round(rect.width)
        canvas.height = Math.round(rect.height)
        cursorRef.current = { x: canvas.width / 2, y: canvas.height / 2 }
      }
      const delta = Math.min(50, now - previous)
      previous = now
      context.fillStyle = '#070a0d'
      context.fillRect(0, 0, canvas.width, canvas.height)
      context.strokeStyle = 'rgba(255,255,255,.035)'
      for (let x = 0; x < canvas.width; x += 48) { context.beginPath(); context.moveTo(x, 0); context.lineTo(x, canvas.height); context.stroke() }
      for (let y = 0; y < canvas.height; y += 48) { context.beginPath(); context.moveTo(0, y); context.lineTo(canvas.width, y); context.stroke() }
      if (runningRef.current) {
        const elapsed = now - metricsRef.current.startedAt
        setTimeLeft(Math.max(0, Math.ceil((20_000 - elapsed) / 1000)))
        if (elapsed >= 20_000) { finish(); return }
        if (!targetRef.current && now >= nextTargetAtRef.current) spawnTarget(canvas, now)
        const target = targetRef.current
        if (target && mode === 'tracking') {
          target.x += target.vx * delta / 1000; target.y += target.vy * delta / 1000
          if (target.x < target.radius || target.x > canvas.width - target.radius) target.vx *= -1
          if (target.y < target.radius || target.y > canvas.height - target.radius) target.vy *= -1
          if (distance(cursorRef.current, target) <= target.radius) metricsRef.current.trackingMs += delta
        }
      }
      const target = targetRef.current
      if (target) {
        const gradient = context.createRadialGradient(target.x, target.y, 2, target.x, target.y, target.radius * 1.8)
        gradient.addColorStop(0, '#d9ff43'); gradient.addColorStop(.45, '#d9ff43'); gradient.addColorStop(1, 'rgba(217,255,67,0)')
        context.fillStyle = gradient; context.beginPath(); context.arc(target.x, target.y, target.radius * 1.8, 0, Math.PI * 2); context.fill()
        context.fillStyle = '#0a0d0f'; context.beginPath(); context.arc(target.x, target.y, 5, 0, Math.PI * 2); context.fill()
      }
      const cursor = cursorRef.current
      context.strokeStyle = '#f4f7ef'; context.lineWidth = 1.5
      context.beginPath(); context.moveTo(cursor.x - 10, cursor.y); context.lineTo(cursor.x - 3, cursor.y); context.moveTo(cursor.x + 3, cursor.y); context.lineTo(cursor.x + 10, cursor.y); context.moveTo(cursor.x, cursor.y - 10); context.lineTo(cursor.x, cursor.y - 3); context.moveTo(cursor.x, cursor.y + 3); context.lineTo(cursor.x, cursor.y + 10); context.stroke()
      frameRef.current = requestAnimationFrame(draw)
    }
    frameRef.current = requestAnimationFrame(draw)
    return () => cancelAnimationFrame(frameRef.current)
  }, [finish, mode, spawnTarget])

  useEffect(() => {
    const move = (event: MouseEvent) => {
      if (!runningRef.current) return
      const canvas = canvasRef.current
      if (!canvas) return
      cursorRef.current.x = Math.max(0, Math.min(canvas.width, cursorRef.current.x + event.movementX))
      cursorRef.current.y = Math.max(0, Math.min(canvas.height, cursorRef.current.y + event.movementY))
    }
    document.addEventListener('mousemove', move)
    return () => document.removeEventListener('mousemove', move)
  }, [])

  const start = () => {
    const canvas = canvasRef.current
    if (!canvas) return
    metricsRef.current = { hits: 0, misses: 0, reactionTotal: 0, reactions: 0, trackingMs: 0, startedAt: performance.now() }
    targetRef.current = null
    nextTargetAtRef.current = mode === 'reaction' ? performance.now() + 800 + Math.random() * 1800 : performance.now()
    runningRef.current = true; setRunning(true); setResult(undefined); setTimeLeft(20)
    void canvas.requestPointerLock()
  }
  const shoot = () => {
    if (!runningRef.current || mode === 'tracking') return
    const canvas = canvasRef.current
    const target = targetRef.current
    if (!canvas || !target) { metricsRef.current.misses++; return }
    if (distance(cursorRef.current, target) <= target.radius) {
      metricsRef.current.hits++; metricsRef.current.reactions++; metricsRef.current.reactionTotal += performance.now() - target.bornAt
      targetRef.current = null
      nextTargetAtRef.current = mode === 'reaction' ? performance.now() + 700 + Math.random() * 1600 : performance.now() + (mode === 'switching' ? 80 : 180)
    } else metricsRef.current.misses++
  }
  return (
    <>
      <Link to={cs2Path(organizationId, 'training')} className="cs2-back-link">← Training</Link>
      <PageHeader eyebrow="BROWSER AIM MVP" title="Aim Range" text="20 Sekunden. Pointer Lock. Messbar und direkt im Profil gespeichert." />
      <div className="cs2-aim-tabs">{(['flick', 'reaction', 'switching', 'tracking'] as AimMode[]).map((value) => <button key={value} className={mode === value ? 'is-active' : ''} onClick={() => setParams({ mode: value })} disabled={running}>{value}</button>)}</div>
      <section className="cs2-aim-shell">
        <div className="cs2-aim-hud"><span><Clock3 /> {timeLeft}s</span><b>{mode.toUpperCase()}</b><span>{running ? 'POINTER LOCKED' : 'READY'}</span></div>
        <canvas ref={canvasRef} onMouseDown={shoot} onClick={() => !running && start()} />
        {!running && !result && <div className="cs2-aim-overlay"><Crosshair /><h2>CLICK TO START</h2><p>Maus bewegen · Linksklick treffen · ESC beendet Pointer Lock</p></div>}
        {result && <div className="cs2-aim-overlay result"><Trophy /><h2>SESSION COMPLETE</h2><div><span><b>{result.hits}</b> HITS</span><span><b>{result.accuracy.toFixed(0)}%</b> ACC</span><span><b>{mode === 'tracking' ? `${result.tracking.toFixed(0)}%` : `${result.reaction.toFixed(0)}ms`}</b> {mode === 'tracking' ? 'TRACK' : 'REACTION'}</span></div><button onClick={start}>NOCHMAL</button></div>}
      </section>
    </>
  )
}

function MatchCard({ match, featured = false }: { match: Cs2MatchSummary; featured?: boolean }) {
  const organizationId = useOrganizationId()
  const content = (
    <>
      <div className="cs2-match-card__map"><span>{match.mapName?.slice(0, 2).toUpperCase() ?? 'CS'}</span></div>
      <div className="cs2-match-card__info"><span>{match.status === 'Completed' ? formatDate(match.playedAt) : match.status.toUpperCase()}</span><h3>{match.mapName ?? match.originalFileName}</h3><p>{match.teamAName ?? 'Demo wird analysiert'} vs. {match.teamBName ?? 'CSDA'}</p></div>
      {match.status === 'Completed' ? <><div className={`cs2-result-pill ${match.win ? 'win' : 'loss'}`}>{match.win ? 'WIN' : 'LOSS'}</div><div className="cs2-match-score"><b>{match.teamAScore}</b><i>:</i><b>{match.teamBScore}</b></div><ChevronRight /></> : <ImportStatus match={match} />}
    </>
  )
  return match.status === 'Completed' ? <Link to={cs2Path(organizationId, `matches/${match.id}`)} className={`cs2-match-card ${featured ? 'featured' : ''}`}>{content}</Link> : <div className={`cs2-match-card ${featured ? 'featured' : ''}`}>{content}</div>
}

function ImportStatus({ match }: { match: Cs2MatchSummary }) {
  return <div className={`cs2-import-status ${match.status.toLowerCase()}`}>{match.status === 'Failed' ? <X /> : <RefreshCw />}<span>{match.status}<small>{match.failureMessage ?? 'Background job läuft'}</small></span></div>
}

function ProcessingMatch({ match }: { match: Cs2MatchSummary }) {
  const organizationId = useOrganizationId()
  return <div className="cs2-processing-page"><ImportStatus match={match} /><h1>{match.originalFileName}</h1><p>Du kannst die Seite verlassen. Der Import läuft unabhängig vom HTTP-Request weiter.</p><Link to={cs2Path(organizationId, 'matches')} className="cs2-button cs2-button--ghost">Zu allen Matches</Link></div>
}

function HighlightCard({ highlight, compact = false, onReact }: { highlight: Cs2Highlight; compact?: boolean; onReact?: (value: string) => void }) {
  return <article className={`cs2-highlight-card ${compact ? 'compact' : ''}`}><div className="cs2-highlight-card__score">{highlight.score}</div><span>ROUND {highlight.roundNumber} · {highlight.type}</span><h3>{highlight.title}</h3><p>{highlight.playerName}</p>{!compact && <div className="cs2-reactions">{['🔥', '😂', '💀', '🤡'].map((value) => <button key={value} onClick={() => onReact?.(value)}>{value} <small>{highlight.reactions?.find((item) => item.reaction === value)?.count ?? 0}</small></button>)}</div>}</article>
}

function Leaderboard({ title, icon, players, metric, detail }: { title: string; icon: ReactNode; players: Cs2Leaderboards['performance']; metric: (player: Cs2Leaderboards['performance'][number]) => string; detail: (player: Cs2Leaderboards['performance'][number]) => string }) {
  return <section className="cs2-leaderboard"><div className="cs2-leaderboard__head">{icon}<h2>{title}</h2></div>{players.slice(0, 5).map((player, index) => <div key={player.userId} className="cs2-leaderboard__row"><span>{index + 1}</span><Avatar name={player.displayName} url={player.avatarUrl} /><div><b>{player.displayName}</b><small>{detail(player)}</small></div><strong>{metric(player)}</strong></div>)}{!players.length && <p className="cs2-muted">Noch keine Season-Daten.</p>}</section>
}

function PageHeader({ eyebrow, title, text }: { eyebrow: string; title: string; text: string }) {
  return <header className="cs2-page-header"><span>{eyebrow}</span><h1>{title}</h1><p>{text}</p></header>
}
function StatTile({ label, value, tone }: { label: string; value: string | number; tone?: string }) { return <div className={`cs2-stat-tile ${tone ?? ''}`}><span>{label}</span><b>{value}</b></div> }
function SquadRecordCard({ eyebrow, title, text, record, featured = false }: { eyebrow: string; title: string; text: string; record: { matches: number; wins: number; losses: number; winRate: number }; featured?: boolean }) { return <article className={featured ? 'featured' : ''}><span>{eyebrow}</span><h2>{title}</h2><div><b className="wins">{record.wins}W</b><i>—</i><b className="losses">{record.losses}L</b></div><p>{record.matches} Matches · {record.winRate.toFixed(0)}% Winrate</p><small>{text}</small></article> }
function StatusPill({ status }: { status: Cs2Availability }) { return <span className={`cs2-status-pill ${status.toLowerCase()}`}>{status === 'Yes' ? 'DABEI' : status === 'Maybe' ? 'VIELLEICHT' : 'RAUS'}</span> }
function Avatar({ name, url }: { name: string; url?: string }) { return url ? <img className="cs2-avatar" src={url} alt="" /> : <span className="cs2-avatar cs2-avatar--fallback">{name.slice(0, 2).toUpperCase()}</span> }
function EmptyCard({ text }: { text: string }) { return <div className="cs2-empty-card"><Crosshair /><p>{text}</p></div> }
function Cs2Loading({ label }: { label: string }) { return <div className="cs2-loading"><Crosshair /><span>{label}</span></div> }
function Cs2BootScreen() { return <div className="cs2-root"><Cs2Loading label="BOOTING COUCHCLASH CS2" /></div> }
function Cs2Error({ error }: { error: unknown }) { const message = error instanceof ApiError ? error.message : error instanceof Error ? error.message : 'Unbekannter Fehler'; return <div className="cs2-error"><X /><div><b>Das ging daneben.</b><span>{message}</span></div></div> }
function useOrganizationId() { return useParams().organizationId ?? '' }
function formatDate(value?: string) { return value ? new Intl.DateTimeFormat('de-DE', { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date(value)) : '–' }
function aimModeFor(kind: Cs2TrainingKind) { return kind === 'Reaction' ? 'reaction' : kind === 'TargetSwitching' ? 'switching' : kind === 'Tracking' ? 'tracking' : 'flick' }
function aimKind(mode: AimMode): Cs2TrainingKind { return mode === 'reaction' ? 'Reaction' : mode === 'switching' ? 'TargetSwitching' : mode === 'tracking' ? 'Tracking' : 'Flick' }
function distance(a: { x: number; y: number }, b: { x: number; y: number }) { return Math.hypot(a.x - b.x, a.y - b.y) }
