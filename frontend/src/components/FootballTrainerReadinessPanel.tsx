import { useQuery } from '@tanstack/react-query'
import { AlertTriangle, HeartPulse, ShieldCheck, Users } from 'lucide-react'
import { useLocation } from 'react-router-dom'
import { getCurrentUser } from '../api/auth'
import {
  getFootballSession,
  listFootballProfiles,
  listFootballSessions,
  type FootballAvailabilityStatus,
} from '../api/football'
import { listMembers } from '../api/members'

const statusLabels: Record<FootballAvailabilityStatus, string> = {
  Fit: 'Fit',
  Limited: 'Angeschlagen',
  ReturnToPlay: 'Return-to-Play',
  Injured: 'Verletzt',
}

const statusClasses: Record<FootballAvailabilityStatus, string> = {
  Fit: 'bg-emerald-400/15 text-emerald-200',
  Limited: 'bg-amber-400/15 text-amber-200',
  ReturnToPlay: 'bg-sky-400/15 text-sky-200',
  Injured: 'bg-rose-400/15 text-rose-200',
}

function organizationIdFromPath(pathname: string) {
  const match = pathname.match(/^\/football\/([0-9a-f-]{36})(?:\/|$)/i)
  return match?.[1]
}

export function FootballTrainerReadinessPanel() {
  const location = useLocation()
  const organizationId = organizationIdFromPath(location.pathname)
  const isTrainingPage = /\/football\/[0-9a-f-]{36}\/training(?:\/|$)/i.test(location.pathname)

  const me = useQuery({
    queryKey: ['football-current-user'],
    queryFn: getCurrentUser,
    retry: false,
    enabled: Boolean(organizationId && isTrainingPage),
  })
  const members = useQuery({
    queryKey: ['football-members', organizationId],
    queryFn: () => listMembers(organizationId!),
    enabled: Boolean(organizationId && isTrainingPage && me.data),
  })
  const profiles = useQuery({
    queryKey: ['football-profiles', organizationId],
    queryFn: () => listFootballProfiles(organizationId!),
    enabled: Boolean(organizationId && isTrainingPage && me.data),
  })
  const sessions = useQuery({
    queryKey: ['football-sessions', organizationId, 'trainer-readiness'],
    queryFn: () => listFootballSessions(organizationId!, new Date().toISOString()),
    enabled: Boolean(organizationId && isTrainingPage && me.data),
  })

  const currentMember = members.data?.find((member) => member.userId === me.data?.id)
  const currentProfile = profiles.data?.find((profile) => profile.memberId === currentMember?.id)
  const nextTraining = sessions.data?.find((session) => session.kind === 'Training')
  const detail = useQuery({
    queryKey: ['football-session', organizationId, nextTraining?.id],
    queryFn: () => getFootballSession(organizationId!, nextTraining!.id),
    enabled: Boolean(organizationId && isTrainingPage && currentProfile?.teamRole === 'Coach' && nextTraining?.id),
  })

  if (!organizationId || !isTrainingPage || currentProfile?.teamRole !== 'Coach' || !nextTraining || !detail.data) {
    return null
  }

  const acceptedIds = new Set(
    detail.data.attendance
      .filter((attendance) => attendance.status === 'Accepted')
      .map((attendance) => attendance.memberId),
  )
  const acceptedMembers = (members.data ?? []).filter((member) => acceptedIds.has(member.id))
  const restricted = acceptedMembers.filter((member) => {
    const availability = detail.data?.availability.find((item) => item.memberId === member.id)
    return availability && (availability.status !== 'Fit' || availability.maxLoadPercent < 100)
  })
  const injured = restricted.filter((member) =>
    detail.data?.availability.find((item) => item.memberId === member.id)?.status === 'Injured',
  )

  return (
    <section className="fixed bottom-4 left-4 z-40 hidden w-[min(520px,calc(100vw-2rem))] rounded-2xl border border-white/10 bg-slate-950/95 p-4 shadow-2xl backdrop-blur xl:block">
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 text-emerald-300">
            <HeartPulse size={18} />
            <span className="text-xs font-black uppercase tracking-[0.16em]">Trainer-Check</span>
          </div>
          <p className="mt-1 font-black text-white">{nextTraining.title}</p>
          <p className="mt-1 text-xs text-slate-500">{acceptedMembers.length} Zusagen · {restricted.length} mit Einschränkung</p>
        </div>
        <div className={`flex items-center gap-1.5 rounded-full px-3 py-1.5 text-xs font-bold ${injured.length ? 'bg-rose-400/15 text-rose-200' : restricted.length ? 'bg-amber-400/15 text-amber-200' : 'bg-emerald-400/15 text-emerald-200'}`}>
          {restricted.length ? <AlertTriangle size={14} /> : <ShieldCheck size={14} />}
          {restricted.length ? `${restricted.length} prüfen` : 'Kader fit'}
        </div>
      </div>

      <div className="mt-4 max-h-64 space-y-2 overflow-y-auto pr-1">
        {acceptedMembers.map((member) => {
          const availability = detail.data.availability.find((item) => item.memberId === member.id)
          const profile = profiles.data?.find((item) => item.memberId === member.id)
          const status = availability?.status ?? 'Fit'
          const maxLoad = availability?.maxLoadPercent ?? 100
          return (
            <div key={member.id} className="flex items-center justify-between gap-3 rounded-xl bg-white/5 px-3 py-2.5">
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <p className="truncate text-sm font-bold text-slate-100">{member.displayName}</p>
                  <span className={`rounded-full px-2 py-0.5 text-[11px] font-bold ${statusClasses[status]}`}>{statusLabels[status]}</span>
                </div>
                <p className="mt-1 truncate text-xs text-slate-500">
                  {profile?.position ?? 'Position offen'}
                  {availability?.note ? ` · ${availability.note}` : ''}
                </p>
              </div>
              <div className="shrink-0 text-right">
                <p className={`text-sm font-black ${maxLoad < 100 ? 'text-amber-200' : 'text-emerald-300'}`}>{maxLoad}%</p>
                <p className="text-[11px] text-slate-500">max. Load</p>
              </div>
            </div>
          )
        })}
        {!acceptedMembers.length && (
          <div className="flex items-center gap-2 rounded-xl bg-white/5 p-3 text-sm text-slate-500">
            <Users size={16} /> Noch keine Zusagen für das nächste Training.
          </div>
        )}
      </div>
    </section>
  )
}
