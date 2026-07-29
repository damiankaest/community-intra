import { useRef, useState, type DragEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Boxes,
  CloudDownload,
  Factory,
  FileArchive,
  HardDriveUpload,
  MapPin,
  PackageOpen,
  Plus,
  Server,
  Trash2,
  Zap,
} from 'lucide-react'
import { useParams } from 'react-router-dom'
import type { CurrentUser } from '../api/auth'
import {
  createFactory,
  deleteFactory,
  getFactoryInsights,
  importServerSave,
  uploadSaveFile,
  type DetectedFactoryArea,
  type SaveFactoryInput,
} from '../api/factoryInsights'
import { FeatureLayout } from './PhaseSixPages'

export function FactoryInsightsPage({ user }: { user: CurrentUser }) {
  const { organizationId = '' } = useParams()
  const queryClient = useQueryClient()
  const fileInput = useRef<HTMLInputElement>(null)
  const [dragging, setDragging] = useState(false)
  const [factoryName, setFactoryName] = useState('')
  const overview = useQuery({
    queryKey: ['factory-insights', organizationId],
    queryFn: () => getFactoryInsights(organizationId),
    enabled: Boolean(organizationId),
    retry: false,
  })
  const invalidate = () =>
    queryClient.invalidateQueries({
      queryKey: ['factory-insights', organizationId],
    })
  const upload = useMutation({
    mutationFn: (file: File) => uploadSaveFile(organizationId, file),
    onSuccess: invalidate,
  })
  const serverImport = useMutation({
    mutationFn: () => importServerSave(organizationId),
    onSuccess: invalidate,
  })
  const register = useMutation({
    mutationFn: (input: SaveFactoryInput) =>
      createFactory(organizationId, input),
    onSuccess: async () => {
      setFactoryName('')
      await invalidate()
    },
  })
  const remove = useMutation({
    mutationFn: (factoryId: string) =>
      deleteFactory(organizationId, factoryId),
    onSuccess: invalidate,
  })
  const latest = overview.data?.latestSnapshot
  const analysis = latest?.analysis
  const error =
    overview.error ??
    upload.error ??
    serverImport.error ??
    register.error ??
    remove.error

  const acceptFile = (file?: File) => {
    if (file) upload.mutate(file)
  }
  const onDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault()
    setDragging(false)
    acceptFile(event.dataTransfer.files[0])
  }

  return (
    <FeatureLayout
      user={user}
      title="Fabrikarchiv"
      subtitle="Spielstand rein, Fabriklage raus – ohne Tabellenpflege nach Feierabend."
    >
      {error && (
        <div className="mb-5 rounded-xl border border-red-400/30 bg-red-400/10 p-4 text-sm text-red-100">
          {error.message}
        </div>
      )}

      <section className="overflow-hidden rounded-3xl border border-[var(--theme-primary)]/25 bg-[linear-gradient(135deg,rgba(245,158,11,.14),rgba(15,23,42,.8)_55%,rgba(34,197,94,.08))] p-5 shadow-2xl shadow-black/20 md:p-7">
        <div className="grid gap-6 xl:grid-cols-[1.1fr_.9fr]">
          <div>
            <div className="flex items-center gap-2 text-xs font-black tracking-[0.16em] text-[var(--theme-primary)] uppercase">
              <Factory size={18} />
              Phase 14 · Save-Analyse
            </div>
            <h2 className="mt-3 max-w-xl text-2xl font-black text-white md:text-3xl">
              Zeig dem Intranet eure Welt.
            </h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-[var(--theme-muted)]">
              Importiere jetzt eine lokale <strong>.sav</strong>. Sobald dein
              ByteBlitz-Token da ist, lädt „Vom Server holen“ denselben
              Spielstand automatisch. Gespeichert werden nur Analyse und Hash,
              nicht die Binärdatei.
            </p>
            <div className="mt-5 flex flex-wrap gap-3">
              <button
                type="button"
                className="inline-flex items-center gap-2 rounded-xl bg-[var(--theme-primary)] px-4 py-3 text-sm font-black text-black transition hover:brightness-110 disabled:opacity-50"
                disabled={upload.isPending || !overview.data?.saveParserAvailable}
                onClick={() => fileInput.current?.click()}
              >
                <HardDriveUpload size={18} />
                {upload.isPending ? 'Save wird analysiert …' : 'Save auswählen'}
              </button>
              <button
                type="button"
                className="inline-flex items-center gap-2 rounded-xl border border-white/15 bg-white/[0.06] px-4 py-3 text-sm font-bold text-white transition hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-40"
                disabled={
                  serverImport.isPending ||
                  overview.data?.serverState !== 'Online' ||
                  !overview.data?.saveParserAvailable
                }
                onClick={() => serverImport.mutate()}
              >
                <CloudDownload size={18} />
                {serverImport.isPending
                  ? 'Server-Save wird geladen …'
                  : 'Vom Server holen'}
              </button>
              <input
                ref={fileInput}
                className="sr-only"
                type="file"
                accept=".sav"
                onChange={(event) => acceptFile(event.target.files?.[0])}
              />
            </div>
            {overview.data?.serverState !== 'Online' && (
              <p className="mt-3 flex items-center gap-2 text-xs text-[var(--theme-muted)]">
                <Server size={14} />
                Automatik wartet noch: {overview.data?.serverMessage}
              </p>
            )}
          </div>
          <div
            className={`flex min-h-48 flex-col items-center justify-center rounded-2xl border border-dashed p-6 text-center transition ${
              dragging
                ? 'border-[var(--theme-primary)] bg-[var(--theme-primary)]/10'
                : 'border-white/15 bg-black/20'
            }`}
            onDragEnter={(event) => {
              event.preventDefault()
              setDragging(true)
            }}
            onDragOver={(event) => event.preventDefault()}
            onDragLeave={() => setDragging(false)}
            onDrop={onDrop}
          >
            <FileArchive
              size={36}
              className="text-[var(--theme-primary)]"
            />
            <strong className="mt-3 text-white">
              .sav hier fallen lassen
            </strong>
            <span className="mt-1 text-xs text-[var(--theme-muted)]">
              maximal 200 MB · Rohdatei wird verworfen
            </span>
          </div>
        </div>
      </section>

      {!overview.data?.saveParserAvailable && (
        <div className="mt-5 rounded-xl border border-amber-400/30 bg-amber-400/10 p-4 text-sm text-amber-100">
          Der interne Save-Parser ist nicht erreichbar. Prüfe beim Deployment
          den neuen Container <code>save-parser</code>.
        </div>
      )}

      {analysis ? (
        <>
          <section className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
            <Metric
              icon={<Boxes size={19} />}
              value={analysis.totals.buildables}
              label="Bauwerke"
            />
            <Metric
              icon={<Factory size={19} />}
              value={analysis.totals.productionMachines}
              label="Produktionsmaschinen"
            />
            <Metric
              icon={<Zap size={19} />}
              value={analysis.totals.powerBuildings}
              label="Stromgebäude"
            />
            <Metric
              icon={<PackageOpen size={19} />}
              value={analysis.totals.logistics}
              label="Logistik"
            />
            <Metric
              icon={<MapPin size={19} />}
              value={analysis.detectedAreas.length}
              label="Fabrikbereiche"
            />
          </section>

          <div className="mt-5 grid gap-5 xl:grid-cols-[1.15fr_.85fr]">
            <section className="rounded-2xl border border-white/10 bg-white/[0.035] p-5">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <h2 className="text-lg font-black text-white">
                    Erkannte Fabrikbereiche
                  </h2>
                  <p className="mt-1 text-sm text-[var(--theme-muted)]">
                    Räumlich gruppierte Maschinen – ein Klick macht daraus eine
                    benannte Fabrik.
                  </p>
                </div>
                <span className="rounded-full bg-[var(--theme-primary)]/10 px-3 py-1 text-xs font-bold text-[var(--theme-primary)]">
                  {latest?.saveName ?? latest?.originalFileName}
                </span>
              </div>
              <div className="mt-4 grid gap-3 md:grid-cols-2">
                {analysis.detectedAreas.map((area) => (
                  <DetectedAreaCard
                    key={area.key}
                    area={area}
                    disabled={register.isPending}
                    onRegister={() =>
                      register.mutate({
                        name: area.suggestedName,
                        description: area.topBuildingTypes
                          .slice(0, 3)
                          .map((item) => item.displayName)
                          .join(', '),
                        centerX: area.centerX,
                        centerY: area.centerY,
                        radiusMeters: area.radiusMeters,
                      })
                    }
                  />
                ))}
                {analysis.detectedAreas.length === 0 && (
                  <p className="rounded-xl border border-dashed border-white/10 p-5 text-sm text-[var(--theme-muted)]">
                    Noch kein Bereich mit mindestens drei nahen Maschinen
                    erkannt.
                  </p>
                )}
              </div>
            </section>

            <section className="rounded-2xl border border-white/10 bg-white/[0.035] p-5">
              <h2 className="text-lg font-black text-white">
                Häufigste Bauwerke
              </h2>
              <p className="mt-1 text-sm text-[var(--theme-muted)]">
                Die zehn größten Posten im aktuellen Snapshot.
              </p>
              <div className="mt-4 space-y-2">
                {analysis.buildingTypes.slice(0, 10).map((building) => (
                  <div
                    key={building.typePath}
                    className="flex items-center justify-between gap-4 rounded-lg bg-black/20 px-3 py-2"
                  >
                    <div className="min-w-0">
                      <strong className="block truncate text-sm text-white">
                        {building.displayName}
                      </strong>
                      <span className="text-[10px] tracking-wide text-[var(--theme-muted)] uppercase">
                        {building.category}
                      </span>
                    </div>
                    <b className="text-[var(--theme-primary)]">
                      {building.count}
                    </b>
                  </div>
                ))}
              </div>
            </section>
          </div>
        </>
      ) : (
        <section className="mt-5 rounded-2xl border border-dashed border-white/10 bg-white/[0.025] p-8 text-center">
          <FileArchive
            className="mx-auto text-[var(--theme-primary)]"
            size={32}
          />
          <h2 className="mt-3 font-black text-white">
            Noch kein Spielstand im Archiv
          </h2>
          <p className="mt-2 text-sm text-[var(--theme-muted)]">
            Der erste Import baut automatisch eure Weltübersicht.
          </p>
        </section>
      )}

      <div className="mt-5 grid gap-5 lg:grid-cols-2">
        <section className="rounded-2xl border border-white/10 bg-white/[0.035] p-5">
          <h2 className="text-lg font-black text-white">Eure Fabriken</h2>
          <form
            className="mt-4 flex gap-2"
            onSubmit={(event) => {
              event.preventDefault()
              if (factoryName.trim()) {
                register.mutate({ name: factoryName.trim() })
              }
            }}
          >
            <input
              className="min-w-0 flex-1 rounded-xl border border-white/10 bg-black/25 px-4 py-2.5 text-sm text-white outline-none focus:border-[var(--theme-primary)]"
              value={factoryName}
              maxLength={120}
              placeholder="z. B. Aluminium-Wunderland"
              onChange={(event) => setFactoryName(event.target.value)}
            />
            <button
              type="submit"
              disabled={!factoryName.trim() || register.isPending}
              className="rounded-xl bg-white/10 px-4 text-white hover:bg-white/15 disabled:opacity-40"
              aria-label="Fabrik anlegen"
            >
              <Plus size={18} />
            </button>
          </form>
          <div className="mt-4 space-y-2">
            {overview.data?.factories.map((factory) => (
              <article
                key={factory.id}
                className="flex items-center gap-3 rounded-xl border border-white/8 bg-black/20 p-3"
              >
                <span className="flex size-9 items-center justify-center rounded-lg bg-[var(--theme-primary)]/10 text-[var(--theme-primary)]">
                  <Factory size={18} />
                </span>
                <div className="min-w-0 flex-1">
                  <strong className="block truncate text-sm text-white">
                    {factory.name}
                  </strong>
                  <span className="text-xs text-[var(--theme-muted)]">
                    {factory.machineCount === undefined
                      ? 'Noch keinem Save-Bereich zugeordnet'
                      : `${factory.machineCount} Maschinen · ${factory.buildableCount} Bauwerke`}
                  </span>
                </div>
                <button
                  type="button"
                  className="rounded-lg p-2 text-[var(--theme-muted)] hover:bg-red-400/10 hover:text-red-300"
                  aria-label={`${factory.name} löschen`}
                  onClick={() => remove.mutate(factory.id)}
                >
                  <Trash2 size={16} />
                </button>
              </article>
            ))}
            {overview.data?.factories.length === 0 && (
              <p className="text-sm text-[var(--theme-muted)]">
                Noch keine Fabrik benannt.
              </p>
            )}
          </div>
        </section>

        <section className="rounded-2xl border border-white/10 bg-white/[0.035] p-5">
          <h2 className="text-lg font-black text-white">Save-Verlauf</h2>
          <div className="mt-4 space-y-2">
            {overview.data?.recentSnapshots.map((snapshot, index) => (
              <article
                key={snapshot.id}
                className="flex items-center gap-3 rounded-xl bg-black/20 p-3"
              >
                <span className="text-[var(--theme-primary)]">
                  {snapshot.source === 'ServerApi' ? (
                    <CloudDownload size={18} />
                  ) : (
                    <HardDriveUpload size={18} />
                  )}
                </span>
                <div className="min-w-0 flex-1">
                  <strong className="block truncate text-sm text-white">
                    {snapshot.saveName ?? snapshot.originalFileName}
                  </strong>
                  <span className="text-xs text-[var(--theme-muted)]">
                    {formatDate(snapshot.importedAt)} ·{' '}
                    {formatBytes(snapshot.fileSizeBytes)}
                  </span>
                </div>
                {index === 0 && (
                  <span className="rounded-full bg-emerald-400/10 px-2 py-1 text-[10px] font-bold text-emerald-300 uppercase">
                    aktuell
                  </span>
                )}
              </article>
            ))}
            {overview.data?.recentSnapshots.length === 0 && (
              <p className="text-sm text-[var(--theme-muted)]">
                Der Verlauf beginnt mit dem ersten Import.
              </p>
            )}
          </div>
        </section>
      </div>
    </FeatureLayout>
  )
}

function Metric({
  icon,
  value,
  label,
}: {
  icon: React.ReactNode
  value: number
  label: string
}) {
  return (
    <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-4">
      <span className="text-[var(--theme-primary)]">{icon}</span>
      <strong className="mt-3 block text-2xl font-black text-white">
        {value.toLocaleString('de-DE')}
      </strong>
      <span className="text-xs text-[var(--theme-muted)]">{label}</span>
    </div>
  )
}

function DetectedAreaCard({
  area,
  disabled,
  onRegister,
}: {
  area: DetectedFactoryArea
  disabled: boolean
  onRegister: () => void
}) {
  return (
    <article className="rounded-xl border border-white/10 bg-black/20 p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <strong className="text-sm text-white">{area.suggestedName}</strong>
          <p className="mt-1 text-xs text-[var(--theme-muted)]">
            {area.machineCount} Maschinen · {area.buildableCount} Bauwerke ·{' '}
            {Math.round(area.radiusMeters)} m
          </p>
        </div>
        <MapPin size={17} className="text-[var(--theme-primary)]" />
      </div>
      <p className="mt-3 line-clamp-2 text-xs leading-5 text-[var(--theme-muted)]">
        {area.topBuildingTypes
          .slice(0, 3)
          .map((item) => `${item.count}× ${item.displayName}`)
          .join(' · ') || 'Unbekannte Zusammenstellung'}
      </p>
      <button
        type="button"
        disabled={disabled}
        onClick={onRegister}
        className="mt-3 inline-flex items-center gap-2 text-xs font-bold text-[var(--theme-primary)] hover:underline disabled:opacity-40"
      >
        <Plus size={14} />
        Als Fabrik registrieren
      </button>
    </article>
  )
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('de-DE', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(value))
}

function formatBytes(value: number) {
  return value >= 1024 * 1024
    ? `${(value / 1024 / 1024).toFixed(1)} MB`
    : `${Math.round(value / 1024)} KB`
}
