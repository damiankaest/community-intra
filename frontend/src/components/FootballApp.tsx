import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Activity,
  ArrowLeft,
  BarChart3,
  BookOpen,
  BrainCircuit,
  CalendarDays,
  Check,
  ChevronRight,
  ClipboardList,
  Gauge,
  HeartPulse,
  Home,
  Plus,
  Shield,
  Sparkles,
  Timer,
  Target,
  UserRound,
  UserRoundPlus,
  Users,
  Zap,
} from 'lucide-react'
import { Link, Navigate, Route, Routes, useLocation, useParams } from 'react-router-dom'
import { getCurrentUser } from '../api/auth'
import { listMembers, type Member } from '../api/members'
import { listOrganizations, type OrganizationSummary } from '../api/organizations'

export type FootballRole = 'Trainer' | 'Betreuer' | 'Spieler'
export type PlayerPosition = 'Tor' | 'Abwehr' | 'Mittelfeld' | 'Sturm'
export type ExerciseCategory = 'Stabilität' | 'Kraft' | 'Mobilität' | 'Ausdauer' | 'Schnelligkeit'

interface FootballProfile {
  memberId: string
  role: FootballRole
  position?: PlayerPosition
  number?: number
  description?: string
  strengths: string[]
  weaknesses: string[]
}

interface Exercise {
  id: string
  title: string
  category: ExerciseCategory
  location: 'Platz' | 'Zuhause' | 'Gym' | 'Überall'
  minPlayers: number
  maxPlayers?: number
  duration: number
  intensity: 'Niedrig' | 'Mittel' | 'Hoch'
  focus: string
  equipment: string
  description: string
  rating: number
}

interface TrainingBlock {
  title: string
  minutes: number
  owner: string
  reason: string
}

const seedExercises: Exercise[] = [
  { id: 'ex-1', title: 'Rondo 4 gegen 2', category: 'Ausdauer', location: 'Platz', minPlayers: 6, duration: 12, intensity: 'Mittel', focus: 'Passwinkel, Freilaufen, Gegenpressing', equipment: 'Hütchen, 1 Ball', description: 'Kompaktes Rondo mit Rollenwechsel nach Ballgewinn.', rating: 4.6 },
  { id: 'ex-2', title: 'Copenhagen Plank', category: 'Stabilität', location: 'Überall', minPlayers: 1, duration: 8, intensity: 'Mittel', focus: 'Adduktoren und Beckenstabilität', equipment: 'Bank oder Partner', description: 'Seitstütz mit erhöhtem oberen Bein. Saubere Hüftlinie halten.', rating: 4.8 },
  { id: 'ex-3', title: 'Antritt 5–15 m', category: 'Schnelligkeit', location: 'Platz', minPlayers: 2, duration: 10, intensity: 'Hoch', focus: 'Erster Schritt und Beschleunigung', equipment: 'Hütchen', description: 'Kurze explosive Starts aus wechselnden Ausgangspositionen mit voller Pause.', rating: 4.7 },
  { id: 'ex-4', title: 'Split Squat Isometrisch', category: 'Kraft', location: 'Zuhause', minPlayers: 1, duration: 8, intensity: 'Mittel', focus: 'Einbeinige Kraft und Kniekontrolle', equipment: 'Optional Kurzhanteln', description: '30–45 Sekunden halten, Knie stabil über dem Fuß ausrichten.', rating: 4.4 },
  { id: 'ex-5', title: '90/90 Hip Flow', category: 'Mobilität', location: 'Überall', minPlayers: 1, duration: 6, intensity: 'Niedrig', focus: 'Hüftrotation', equipment: 'Keins', description: 'Kontrollierte Wechsel zwischen Innen- und Außenrotation der Hüfte.', rating: 4.5 },
]

const navItems = [
  { to: '', label: 'Home', icon: Home },
  { to: 'squad', label: 'Mannschaft', icon: Users },
  { to: 'playbook', label: 'Playbook', icon: BookOpen },
  { to: 'training', label: 'Training', icon: ClipboardList },
  { to: 'individual', label: 'Individual', icon: Target },
  { to: 'performance', label: 'Leistung', icon: BarChart3 },
]

export function FootballApp() {
  const me = useQuery({ queryKey: ['football-current-user'], queryFn: getCurrentUser, retry: false })
  if (me.isPending) return <FootballLoading text="Sitzung wird geprüft …" />
  if (!me.data) { window.location.replace('/login'); return <FootballLoading text="Weiterleitung zur Anmeldung …" /> }
  return <Routes><Route path="/football" element={<FootballEntry />} /><Route path="/football/:organizationId/*" element={<FootballWorkspace />} /><Route path="*" element={<Navigate to="/football" replace />} /></Routes>
}

function FootballEntry() {
  const organizations = useQuery({ queryKey: ['football-organizations'], queryFn: listOrganizations })
  return <div className="min-h-screen bg-slate-950 px-5 py-10 text-slate-100"><div className="mx-auto max-w-5xl"><Link to="/organizations" className="inline-flex items-center gap-2 text-sm text-slate-400 hover:text-white"><ArrowLeft size={16}/> Community Intranet</Link><div className="mt-10 rounded-3xl border border-emerald-400/20 bg-gradient-to-br from-emerald-500/15 via-slate-900 to-slate-950 p-8"><div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-emerald-400 text-slate-950"><Shield size={28}/></div><p className="mt-6 text-xs font-black uppercase tracking-[0.2em] text-emerald-300">Football Operations</p><h1 className="mt-2 text-4xl font-black tracking-tight">Mannschaft, Training und Entwicklung an einem Ort.</h1><p className="mt-4 max-w-3xl text-slate-300">Plane Einheiten gemeinsam im Trainerteam, berücksichtige den tatsächlich anwesenden Kader und entwickle Spieler gezielt über Playbook, Individualtraining und Leistungstests.</p></div><h2 className="mt-10 text-lg font-bold">Mannschaft auswählen</h2><div className="mt-4 grid gap-4 md:grid-cols-2">{organizations.data?.map((org) => <OrganizationCard key={org.id} organization={org} />)}</div>{!organizations.isPending && !organizations.data?.length && <p className="mt-4 text-slate-400">Du bist noch keiner Organisation zugeordnet.</p>}</div></div>
}

function OrganizationCard({ organization }: { organization: OrganizationSummary }) {
  return <Link to={`/football/${organization.id}`} className="group rounded-2xl border border-white/10 bg-white/5 p-5 transition hover:border-emerald-400/50 hover:bg-emerald-400/5"><div className="flex items-center justify-between gap-4"><div><p className="font-bold text-white">{organization.name}</p><p className="mt-1 text-sm text-slate-400">{organization.description || 'Als Fußballmannschaft öffnen'}</p></div><ChevronRight className="text-slate-500 transition group-hover:translate-x-1 group-hover:text-emerald-300"/></div></Link>
}

function FootballWorkspace() {
  const { organizationId = '' } = useParams(); const location = useLocation()
  const members = useQuery({ queryKey: ['football-members', organizationId], queryFn: () => listMembers(organizationId), enabled: Boolean(organizationId) })
  const organizations = useQuery({ queryKey: ['football-organizations'], queryFn: listOrganizations }); const organization = organizations.data?.find((item) => item.id === organizationId)
  return <div className="min-h-screen bg-slate-950 text-slate-100"><header className="sticky top-0 z-20 border-b border-white/10 bg-slate-950/90 backdrop-blur"><div className="mx-auto flex max-w-7xl items-center justify-between gap-4 px-4 py-3 sm:px-6"><div className="flex items-center gap-3"><div className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-400 text-slate-950"><Shield size={21}/></div><div><p className="font-black">{organization?.name || 'Football'}</p><p className="text-xs text-slate-500">Training & Performance</p></div></div><Link to="/organizations" className="text-sm text-slate-400 hover:text-white">Community Intranet</Link></div></header><div className="mx-auto grid max-w-7xl gap-6 px-4 py-6 sm:px-6 lg:grid-cols-[210px_1fr]"><aside className="flex gap-2 overflow-x-auto lg:block lg:space-y-1">{navItems.map(({ to, label, icon: Icon }) => { const target = `/football/${organizationId}${to ? `/${to}` : ''}`; const active = location.pathname === target || (to && location.pathname.startsWith(`${target}/`)); return <Link key={label} to={target} className={`flex shrink-0 items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-semibold ${active ? 'bg-emerald-400 text-slate-950' : 'text-slate-400 hover:bg-white/5 hover:text-white'}`}><Icon size={17}/>{label}</Link> })}</aside><main className="min-w-0"><Routes><Route index element={<Dashboard members={members.data || []} />} /><Route path="squad" element={<SquadPage members={members.data || []} organizationId={organizationId} />} /><Route path="playbook" element={<PlaybookPage />} /><Route path="training" element={<TrainingPage members={members.data || []} />} /><Route path="individual" element={<IndividualPage members={members.data || []} />} /><Route path="performance" element={<PerformancePage members={members.data || []} />} /></Routes></main></div></div>
}

function Dashboard({ members }: { members: Member[] }) {
  const accepted = Math.max(0, Math.min(members.length, 14)); return <div className="space-y-6"><PageHeading eyebrow="Heute im Blick" title="Mannschaftszentrale" text="Termine, Verfügbarkeit und der nächste sportliche Schwerpunkt auf einen Blick." /><div className="grid gap-4 xl:grid-cols-2"><EventCard type="Spiel" title="Nächstes Ligaspiel" meta="Samstag · 15:00 · Heim" detail="Gegnerform: S · S · N · U · S" accent="emerald" /><EventCard type="Training" title="Umschalten nach Ballverlust" meta="Donnerstag · 19:30" detail="Fokus: Gegenpressing, kurze Sprints, Restverteidigung" accent="cyan" /></div><div className="grid gap-4 md:grid-cols-3"><Stat label="Zugesagt" value={`${accepted}`} hint="für die nächste Einheit" icon={<Check/>}/><Stat label="Offen" value={`${Math.max(0, members.length - accepted)}`} hint="Antworten fehlen" icon={<CalendarDays/>}/><Stat label="Kader" value={`${members.length}`} hint="aktive Mitglieder" icon={<Users/>}/></div><Panel title="Trainer-Hinweis" icon={<BrainCircuit/>}><p className="text-sm leading-6 text-slate-300">Die Trainingsplanung soll nicht nur nach Teilnehmerzahl reagieren, sondern nach Rollen und Profilen: sechs Stürmer benötigen einen anderen Plan als sechs Abwehrspieler. Genau diese Kaderzusammensetzung wird im Trainingsplaner sichtbar gemacht.</p></Panel></div>
}

function SquadPage({ members, organizationId }: { members: Member[]; organizationId: string }) {
  const [profiles, setProfiles] = useStoredProfiles(organizationId, members); const update = (memberId: string, patch: Partial<FootballProfile>) => setProfiles((all) => ({ ...all, [memberId]: { ...all[memberId], ...patch } }))
  return <div className="space-y-6"><PageHeading eyebrow="Kader" title="Profile & Rollen" text="Trainer, Betreuer und Spieler erhalten sportliche Profile mit Position, Stärken, Schwächen und Beschreibung." action={<button className="football-button"><UserRoundPlus size={16}/> Spieler einladen</button>} /><div className="grid gap-4 xl:grid-cols-2">{members.map((member) => { const profile = profiles[member.id]; return <div key={member.id} className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="flex items-start gap-4"><Avatar member={member}/><div className="min-w-0 flex-1"><div className="flex flex-wrap items-center gap-2"><h3 className="font-bold">{member.displayName}</h3><span className="football-chip">{profile?.role || 'Spieler'}</span></div><p className="mt-1 text-sm text-slate-500">{member.email}</p></div></div><div className="mt-5 grid gap-3 sm:grid-cols-2"><Select label="Teamrolle" value={profile?.role || 'Spieler'} onChange={(value) => update(member.id, { role: value as FootballRole })} options={['Trainer','Betreuer','Spieler']} /><Select label="Position" value={profile?.position || 'Mittelfeld'} onChange={(value) => update(member.id, { position: value as PlayerPosition })} options={['Tor','Abwehr','Mittelfeld','Sturm']} /></div><label className="mt-3 block text-xs font-bold uppercase tracking-wide text-slate-500">Kurzbeschreibung</label><textarea className="football-input mt-2 min-h-20" value={profile?.description || ''} onChange={(e) => update(member.id, { description: e.target.value })} placeholder="Spielstil, bevorzugte Rolle, Hinweise …"/><div className="mt-3 grid gap-3 sm:grid-cols-2"><TagEditor label="Stärken" values={profile?.strengths || []} onChange={(strengths) => update(member.id,{ strengths })}/><TagEditor label="Entwicklungsfelder" values={profile?.weaknesses || []} onChange={(weaknesses) => update(member.id,{ weaknesses })}/></div></div> })}</div></div>
}

function PlaybookPage() {
  const [category, setCategory] = useState<ExerciseCategory | 'Alle'>('Alle'); const [players, setPlayers] = useState(8); const exercises = seedExercises.filter((exercise) => (category === 'Alle' || exercise.category === category) && exercise.minPlayers <= players && (!exercise.maxPlayers || exercise.maxPlayers >= players))
  return <div className="space-y-6"><PageHeading eyebrow="Wissensbasis" title="Playbook" text="Übungen sammeln, filtern, bewerten und später direkt in Trainingseinheiten übernehmen." action={<button className="football-button"><Plus size={16}/> Übung anlegen</button>} /><div className="rounded-2xl border border-white/10 bg-white/5 p-4"><div className="flex flex-wrap gap-2">{(['Alle','Stabilität','Kraft','Mobilität','Ausdauer','Schnelligkeit'] as const).map((item) => <button key={item} onClick={() => setCategory(item)} className={`rounded-full px-3 py-1.5 text-sm font-semibold ${category === item ? 'bg-emerald-400 text-slate-950' : 'bg-white/5 text-slate-300'}`}>{item}</button>)}</div><label className="mt-4 flex max-w-sm items-center gap-3 text-sm text-slate-400">Teilnehmer <input type="range" min="1" max="24" value={players} onChange={(e) => setPlayers(Number(e.target.value))} className="flex-1"/><strong className="text-white">{players}</strong></label></div><div className="grid gap-4 xl:grid-cols-2">{exercises.map((exercise) => <ExerciseCard key={exercise.id} exercise={exercise}/>)}</div></div>
}

function TrainingPage({ members }: { members: Member[] }) {
  const [focus, setFocus] = useState('Umschalten nach Ballverlust'); const [duration, setDuration] = useState(90); const [attending, setAttending] = useState(() => members.slice(0, Math.min(12, members.length)).map((m) => m.id)); const [plan, setPlan] = useState<TrainingBlock[]>([])
  const positions = useMemo(() => attending.map((_, index) => ['Abwehr','Mittelfeld','Sturm','Sturm','Abwehr','Mittelfeld'][index % 6]), [attending])
  const generate = () => { const strikerHeavy = positions.filter((p) => p === 'Sturm').length >= Math.max(3, positions.length / 2); const defenseHeavy = positions.filter((p) => p === 'Abwehr').length >= Math.max(3, positions.length / 2); const tactical = strikerHeavy ? 'Abschluss unter Gegnerdruck + Gegenpressing nach Abschluss' : defenseHeavy ? 'Spielaufbau unter Druck + Restverteidigung' : focus; setPlan([{ title: 'Aktivierung & Mobilität', minutes: 12, owner: 'Co-Trainer', reason: 'Verletzungsprävention und Bewegungsqualität' },{ title: 'Technisches Warm-up mit Ball', minutes: 15, owner: 'Trainer', reason: `Vorbereitung auf ${focus}` },{ title: tactical, minutes: Math.max(25, duration - 52), owner: 'Trainerteam', reason: `${attending.length} Zusagen · Kaderprofil berücksichtigt` },{ title: 'Spielform mit Coachingregeln', minutes: 20, owner: 'Trainer', reason: 'Transfer in spielnahe Entscheidungen' },{ title: 'Cooldown + Spielerfeedback', minutes: 5, owner: 'Betreuer', reason: 'Belastung und Übungsrating erfassen' }]) }
  return <div className="space-y-6"><PageHeading eyebrow="Trainerteam" title="Trainingseinheit planen" text="Planung kollaborativ vorbereiten, Aufgaben verteilen und den Entwurf an Teilnehmerzahl, Positionen und Entwicklungsfelder anpassen." /><div className="grid gap-4 xl:grid-cols-[1fr_360px]"><Panel title="Rahmen" icon={<ClipboardList/>}><div className="grid gap-4 sm:grid-cols-2"><Field label="Fokus"><input className="football-input" value={focus} onChange={(e) => setFocus(e.target.value)}/></Field><Field label="Dauer in Minuten"><input className="football-input" type="number" value={duration} onChange={(e) => setDuration(Number(e.target.value))}/></Field></div><div className="mt-5"><p className="text-xs font-bold uppercase tracking-wide text-slate-500">Wer kommt?</p><div className="mt-3 flex flex-wrap gap-2">{members.map((member) => { const active = attending.includes(member.id); return <button key={member.id} onClick={() => setAttending((ids) => active ? ids.filter((id) => id !== member.id) : [...ids, member.id])} className={`rounded-full px-3 py-1.5 text-sm ${active ? 'bg-emerald-400 text-slate-950' : 'bg-white/5 text-slate-400'}`}>{member.displayName}</button> })}</div></div></Panel><Panel title="Plan-Assistent" icon={<Sparkles/>}><p className="text-sm leading-6 text-slate-300">Der Entwurf nutzt Fokus, verfügbare Zeit und Kaderstruktur. Später kommen Leistungsdaten, Schwächen, Belastung und vergangene Bewertungen hinzu.</p><button onClick={generate} className="football-button mt-5 w-full justify-center"><BrainCircuit size={17}/> KI-Entwurf generieren</button></Panel></div>{plan.length > 0 && <Panel title="Entwurf" icon={<CalendarDays/>}><div className="space-y-3">{plan.map((block, index) => <div key={`${block.title}-${index}`} className="grid gap-3 rounded-xl border border-white/10 bg-black/20 p-4 sm:grid-cols-[64px_1fr_150px]"><div className="font-black text-emerald-300">{block.minutes} min</div><div><p className="font-bold">{block.title}</p><p className="mt-1 text-sm text-slate-500">{block.reason}</p></div><div className="text-sm text-slate-300">{block.owner}</div></div>)}</div><div className="mt-4 flex flex-wrap gap-2"><button className="football-button"><Check size={16}/> Plan übernehmen</button><button className="football-button-secondary">Gemeinsam bearbeiten</button></div></Panel>}</div>
}

function IndividualPage({ members }: { members: Member[] }) { return <div className="space-y-6"><PageHeading eyebrow="Gezielte Entwicklung" title="Individualtraining" text="Kurze zusätzliche Blöcke mit einem Trainer für konkrete Entwicklungsfelder – direkt vor, während oder nach dem Mannschaftstraining." /><div className="grid gap-4 xl:grid-cols-2">{members.slice(0,6).map((member,index) => <div key={member.id} className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="flex items-center gap-3"><Avatar member={member}/><div><p className="font-bold">{member.displayName}</p><p className="text-sm text-slate-500">{['Erster Kontakt unter Druck','Antritt aus Richtungswechsel','Schwacher Fuß','Kopfball-Timing'][index%4]}</p></div></div><div className="mt-4 rounded-xl bg-black/20 p-4"><p className="text-xs font-bold uppercase tracking-wide text-emerald-300">Vorschlag · 12 Minuten</p><p className="mt-2 text-sm text-slate-300">2 min Aktivierung · 6 min fokussierte Wiederholungen · 4 min spielnahe Anwendung</p></div><button className="football-button-secondary mt-4"><Plus size={16}/> Individualblock planen</button></div>)}</div></div> }

function PerformancePage({ members }: { members: Member[] }) { return <div className="space-y-6"><PageHeading eyebrow="Fortschritt" title="Leistungstests" text="Einfache, wiederholbare Tests dokumentieren. Die Architektur ist bereits auf spätere externe Zeitmessung per USB vorbereitet." action={<button className="football-button"><Timer size={16}/> Test starten</button>} /><div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4"><TestCard icon={<Zap/>} title="10 m Sprint" unit="s"/><TestCard icon={<Gauge/>} title="30 m Sprint" unit="s"/><TestCard icon={<Activity/>} title="5-10-5 Agility" unit="s"/><TestCard icon={<HeartPulse/>} title="Yo-Yo / Ausdauer" unit="Level"/></div><Panel title="Spielerfortschritt" icon={<BarChart3/>}><div className="space-y-2">{members.slice(0,8).map((member,index) => <div key={member.id} className="grid grid-cols-[1fr_90px_90px] items-center gap-3 rounded-xl bg-black/20 px-4 py-3 text-sm"><span className="font-semibold">{member.displayName}</span><span className="text-slate-400">10 m {(1.78 + index*0.03).toFixed(2)} s</span><span className="font-bold text-emerald-300">{index%3===0 ? '↑ 2.1%' : 'stabil'}</span></div>)}</div><div className="mt-4 rounded-xl border border-dashed border-white/15 p-4 text-sm text-slate-400"><Timer className="mr-2 inline" size={16}/>Später: USB-Zeitnehmer als optionaler Messadapter. Manuelle Eingabe bleibt immer als Fallback erhalten.</div></Panel></div> }

function EventCard({ type, title, meta, detail, accent }: { type:string; title:string; meta:string; detail:string; accent:'emerald'|'cyan' }) { return <div className="rounded-2xl border border-white/10 bg-white/5 p-6"><span className={`text-xs font-black uppercase tracking-[0.18em] ${accent === 'emerald' ? 'text-emerald-300' : 'text-cyan-300'}`}>{type}</span><h2 className="mt-3 text-2xl font-black">{title}</h2><p className="mt-2 text-slate-300">{meta}</p><p className="mt-5 rounded-xl bg-black/20 p-3 text-sm text-slate-400">{detail}</p><div className="mt-5 flex gap-2"><button className="football-button"><Check size={15}/> Zusagen</button><button className="football-button-secondary">Absagen</button></div></div> }
function ExerciseCard({ exercise }: { exercise: Exercise }) { return <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="flex items-start justify-between gap-4"><div><span className="football-chip">{exercise.category}</span><h3 className="mt-3 text-lg font-black">{exercise.title}</h3></div><span className="text-sm font-bold text-amber-300">★ {exercise.rating}</span></div><p className="mt-3 text-sm leading-6 text-slate-400">{exercise.description}</p><div className="mt-4 grid grid-cols-2 gap-2 text-xs text-slate-300"><span>👥 ab {exercise.minPlayers}</span><span>⏱ {exercise.duration} min</span><span>📍 {exercise.location}</span><span>⚡ {exercise.intensity}</span></div><p className="mt-4 text-sm text-slate-300"><strong>Fokus:</strong> {exercise.focus}</p></div> }
function TestCard({ icon, title, unit }: { icon: React.ReactNode; title:string; unit:string }) { return <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="text-emerald-300">{icon}</div><p className="mt-4 font-black">{title}</p><p className="mt-1 text-sm text-slate-500">Messwert in {unit}</p></div> }
function Stat({ label, value, hint, icon }: {label:string;value:string;hint:string;icon:React.ReactNode}) { return <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="text-emerald-300">{icon}</div><p className="mt-4 text-3xl font-black">{value}</p><p className="mt-1 font-semibold">{label}</p><p className="text-sm text-slate-500">{hint}</p></div> }
function Panel({ title, icon, children }: {title:string;icon:React.ReactNode;children:React.ReactNode}) { return <section className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="mb-4 flex items-center gap-2 font-black"><span className="text-emerald-300">{icon}</span>{title}</div>{children}</section> }
function PageHeading({ eyebrow, title, text, action }: {eyebrow:string;title:string;text:string;action?:React.ReactNode}) { return <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-end"><div><p className="text-xs font-black uppercase tracking-[0.18em] text-emerald-300">{eyebrow}</p><h1 className="mt-2 text-3xl font-black tracking-tight">{title}</h1><p className="mt-2 max-w-3xl text-sm leading-6 text-slate-400">{text}</p></div>{action}</div> }
function FootballLoading({ text }: {text:string}) { return <div className="flex min-h-screen items-center justify-center bg-slate-950 text-slate-400">{text}</div> }
function Avatar({ member }: {member:Member}) { return member.avatarUrl ? <img src={member.avatarUrl} alt="" className="h-11 w-11 rounded-xl object-cover"/> : <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-white/10"><UserRound size={20}/></div> }
function Field({ label, children }: {label:string;children:React.ReactNode}) { return <label className="block"><span className="mb-2 block text-xs font-bold uppercase tracking-wide text-slate-500">{label}</span>{children}</label> }
function Select({ label, value, onChange, options }: {label:string;value:string;onChange:(value:string)=>void;options:string[]}) { return <Field label={label}><select className="football-input" value={value} onChange={(e)=>onChange(e.target.value)}>{options.map((option)=><option key={option} value={option}>{option}</option>)}</select></Field> }
function TagEditor({ label, values, onChange }: {label:string;values:string[];onChange:(values:string[])=>void}) { const [draft,setDraft]=useState(''); return <div><p className="text-xs font-bold uppercase tracking-wide text-slate-500">{label}</p><div className="mt-2 flex flex-wrap gap-1">{values.map((value)=><button key={value} onClick={()=>onChange(values.filter((item)=>item!==value))} className="football-chip">{value} ×</button>)}</div><div className="mt-2 flex gap-2"><input className="football-input" value={draft} onChange={(e)=>setDraft(e.target.value)} placeholder="z. B. Antritt"/><button className="football-button-secondary" onClick={()=>{if(draft.trim()){onChange([...values,draft.trim()]);setDraft('')}}}>+</button></div></div> }

function useStoredProfiles(organizationId: string, members: Member[]) {
  const key = `community-football-profiles:${organizationId}`
  const [profiles, setProfiles] = useState<Record<string, FootballProfile>>(() => { const saved = localStorage.getItem(key); if (saved) { try { return JSON.parse(saved) as Record<string, FootballProfile> } catch { /* ignore */ } } return Object.fromEntries(members.map((member, index) => [member.id, { memberId: member.id, role: index === 0 ? 'Trainer' : 'Spieler', position: ['Tor','Abwehr','Mittelfeld','Sturm'][index % 4] as PlayerPosition, strengths: [], weaknesses: [] }])) })
  const setAndPersist = (updater: (current: Record<string, FootballProfile>) => Record<string, FootballProfile>) => setProfiles((current) => { const next = updater(current); localStorage.setItem(key, JSON.stringify(next)); return next })
  return [profiles, setAndPersist] as const
}
