import { useCallback, useEffect, useState } from 'react'
import {
  analyzeLive,
  getFakeHistory,
  getReadings,
  type FakeHistoryPoint,
  type ReadingsComparisonResponse,
  type ReadingSnapshot,
} from '../api/sensorApi'
import './Dashboard.css'

const MIN_STATIONS = 1
const MAX_STATIONS = 2000

function pickLatestFakePerSensor(
  points: FakeHistoryPoint[],
): Map<string, FakeHistoryPoint> {
  const m = new Map<string, FakeHistoryPoint>()
  for (const p of points) {
    const prev = m.get(p.sensorId)
    if (
      !prev ||
      new Date(p.timestamp).getTime() > new Date(prev.timestamp).getTime()
    ) {
      m.set(p.sensorId, p)
    }
  }
  return m
}

function formatSnapshot(s: ReadingSnapshot | null): {
  time: string
  value: string
} {
  if (!s) {
    return { time: '—', value: '—' }
  }
  return {
    time: new Date(s.timestamp).toLocaleString('sv-SE', {
      dateStyle: 'short',
      timeStyle: 'medium',
    }),
    value: `${s.value.toFixed(1)} ${s.unit}`,
  }
}

type AnomalyStationSnapshot = {
  sensorId: string
  nuTime: string
  nuValue: string
  lastMonthTime: string
  lastMonthValue: string
  testTime: string
  fakeValue: string
}

type AnomalyLogEntry = {
  id: string
  detectedAtIso: string
  detectedAtDisplay: string
  lastMonthHeading: string
  rows: AnomalyStationSnapshot[]
}

function newAnomalyEntryId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID()
  }
  return `anomaly-${Date.now()}-${Math.random().toString(16).slice(2)}`
}

function buildAnomalyRows(
  comparison: ReadingsComparisonResponse,
  fakeBySensor: Map<string, FakeHistoryPoint>,
): AnomalyStationSnapshot[] {
  return comparison.rows.map((r) => {
    const cur = formatSnapshot(r.current)
    const lm = formatSnapshot(r.lastMonth)
    const fake = fakeBySensor.get(r.sensorId)
    const testTime = fake
      ? new Date(fake.timestamp).toLocaleString('sv-SE', {
          dateStyle: 'short',
          timeStyle: 'medium',
        })
      : '—'
    const fakeVal = fake ? `${fake.value.toFixed(1)} °C` : '—'
    return {
      sensorId: r.sensorId,
      nuTime: cur.time,
      nuValue: cur.value,
      lastMonthTime: lm.time,
      lastMonthValue: lm.value,
      testTime,
      fakeValue: fakeVal,
    }
  })
}

/** Förklarar varför antal rader kan skilja från angivet limit (Trafikverket + parsning). */
function buildReadingsMetaLine(
  comparison: ReadingsComparisonResponse,
  fallbackLimit: number,
): string | null {
  if (comparison.measurepointsInResponse == null) return null
  const rows = comparison.rows.length
  const req = comparison.requestedLimit ?? fallbackLimit
  const mp = comparison.measurepointsInResponse
  const skipped = comparison.skippedIncompleteMeasurepoints ?? 0
  const parsed = comparison.parsedObservationCount ?? 0

  const parts: string[] = []
  parts.push(
    `Visar ${rows} stationer (unika stations-id med giltig lufttemperatur). Begärt limit: ${req}.`,
  )
  parts.push(`Senaste svaret innehöll ${mp} mätpunkter i JSON.`)
  if (skipped > 0) {
    parts.push(
      `${skipped} av dem saknade komplett provtid eller lufttemperatur och visas inte i tabellen.`,
    )
  } else {
    parts.push(
      'Alla returnerade mätpunkter hade komplett observation för listning.',
    )
  }
  if (parsed > rows) {
    parts.push(
      `Flera poster med samma stations-id slog samman till senaste värde → ${rows} rader (${parsed} avläsningar ingick).`,
    )
  }
  if (mp < req) {
    parts.push(
      `Trafikverket returnerade färre än ${req} mätpunkter; det styrs av deras API och nyckel, inte av avkortning i appen.`,
    )
  }
  return parts.join(' ')
}

export function Dashboard() {
  const [comparison, setComparison] = useState<ReadingsComparisonResponse | null>(
    null,
  )
  const [isAnomaly, setIsAnomaly] = useState<boolean | null>(null)
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  /** Värde som skickas till API (uppdateras debouncat från fältet). */
  const [stationLimit, setStationLimit] = useState(2000)
  const [stationLimitInput, setStationLimitInput] = useState(2000)
  /** Senaste syntetiska tidpunkt/värde per station (för kolumnerna Test / Fake värde). */
  const [fakeLatestBySensor, setFakeLatestBySensor] = useState<
    Map<string, FakeHistoryPoint>
  >(() => new Map())
  /** Sparade avvikelser (tömms när API inte längre svarar). */
  const [anomalyLog, setAnomalyLog] = useState<AnomalyLogEntry[]>([])

  useEffect(() => {
    const t = window.setTimeout(() => {
      setStationLimit(stationLimitInput)
    }, 500)
    return () => window.clearTimeout(t)
  }, [stationLimitInput])

  const refresh = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const [nextComparison, analysis, fakeHistory] = await Promise.all([
        getReadings(stationLimit),
        analyzeLive(stationLimit),
        getFakeHistory(stationLimit),
      ])
      const fakeMap = pickLatestFakePerSensor(fakeHistory)
      setComparison(nextComparison)
      setFakeLatestBySensor(fakeMap)
      setIsAnomaly(analysis.isAnomaly)
      setLastUpdated(new Date())

      if (analysis.isAnomaly) {
        const detectedAt = new Date()
        setAnomalyLog((prev) => [
          {
            id: newAnomalyEntryId(),
            detectedAtIso: detectedAt.toISOString(),
            detectedAtDisplay: detectedAt.toLocaleString('sv-SE', {
              dateStyle: 'short',
              timeStyle: 'medium',
            }),
            lastMonthHeading: nextComparison.lastMonthHeading,
            rows: buildAnomalyRows(nextComparison, fakeMap),
          },
          ...prev,
        ])
      }
    } catch (e) {
      setAnomalyLog([])
      setIsAnomaly(null)
      setError(e instanceof Error ? e.message : 'Kunde inte hämta data')
    } finally {
      setLoading(false)
    }
  }, [stationLimit])

  useEffect(() => {
    void refresh()
  }, [refresh])

  const readingsMetaLine =
    comparison != null
      ? buildReadingsMetaLine(comparison, stationLimit)
      : null

  return (
    <section className="dashboard" aria-labelledby="dashboard-title">
      <div className="dashboard__toolbar">
        <h1 id="dashboard-title" className="dashboard__title">
          Dashboard
        </h1>
        <button
          type="button"
          className="dashboard__refresh"
          onClick={() => void refresh()}
          disabled={loading}
        >
          {loading ? 'Hämtar…' : 'Uppdatera'}
        </button>
      </div>

      {lastUpdated && (
        <p className="dashboard__meta">
          Senast uppdaterad:{' '}
          <time dateTime={lastUpdated.toISOString()}>
            {lastUpdated.toLocaleString('sv-SE', {
              dateStyle: 'short',
              timeStyle: 'medium',
            })}
          </time>
        </p>
      )}

      <div className="dashboard__stations">
        <div className="dashboard__stations-control">
          <label htmlFor="station-limit" className="dashboard__stations-label">
            Justera max antal stationer
          </label>
          <input
            id="station-limit"
            className="dashboard__stations-input"
            type="number"
            min={MIN_STATIONS}
            max={MAX_STATIONS}
            value={stationLimitInput}
            onChange={(e) => {
              const v = Number.parseInt(e.target.value, 10)
              if (Number.isNaN(v)) {
                setStationLimitInput(MIN_STATIONS)
                return
              }
              setStationLimitInput(
                Math.min(MAX_STATIONS, Math.max(MIN_STATIONS, v)),
              )
            }}
            aria-describedby="station-limit-help"
          />
          <p id="station-limit-help" className="dashboard__stations-help">
            Värdet skickas som <code>limit</code> till Trafikverkets API (1–
            {MAX_STATIONS}). Antalet stationer beror på API-värdet eller
            API-innehållet. Listan uppdateras automatiskt efter en kort paus när
            du ändrat talet, eller direkt via &quot;Uppdatera&quot;.
          </p>
        </div>
      </div>

      {error && (
        <p className="dashboard__error" role="alert">
          {error}
        </p>
      )}

      <div
        className={`dashboard__anomaly ${
          isAnomaly === null
            ? 'dashboard__anomaly--unknown'
            : isAnomaly
              ? 'dashboard__anomaly--alert'
              : 'dashboard__anomaly--ok'
        }`}
        role="status"
        aria-live="polite"
      >
        {isAnomaly === null && !loading && 'Ingen anomali-data ännu'}
        {isAnomaly === null && loading && 'Analyserar…'}
        {isAnomaly === false && 'Ingen avvikelse detekterad'}
        {isAnomaly === true && 'Avvikelse detekterad'}
      </div>

      {anomalyLog.length > 0 && (
        <section
          className="dashboard__anomaly-log"
          aria-labelledby="anomaly-log-title"
        >
          <h2 id="anomaly-log-title" className="dashboard__anomaly-log-title">
            Sparade avvikelser
          </h2>
          <div className="dashboard__anomaly-log-scroll">
            {anomalyLog.map((entry) => (
              <article key={entry.id} className="dashboard__anomaly-card">
                <header className="dashboard__anomaly-card-head">
                  <time dateTime={entry.detectedAtIso}>
                    {entry.detectedAtDisplay}
                  </time>
                  <span className="dashboard__anomaly-card-count">
                    {entry.rows.length} stationer
                  </span>
                </header>
                <ul className="dashboard__anomaly-card-list">
                  {entry.rows.map((row) => (
                    <li
                      key={`${entry.id}-${row.sensorId}`}
                      className="dashboard__anomaly-card-item"
                    >
                      <div className="dashboard__anomaly-card-station">
                        Station {row.sensorId}
                      </div>
                      <dl className="dashboard__anomaly-card-dl">
                        <div>
                          <dt>Nu</dt>
                          <dd>
                            {row.nuTime} · {row.nuValue}
                          </dd>
                        </div>
                        <div>
                          <dt>{entry.lastMonthHeading}</dt>
                          <dd>
                            {row.lastMonthTime} · {row.lastMonthValue}
                          </dd>
                        </div>
                        <div>
                          <dt>Test</dt>
                          <dd>{row.testTime}</dd>
                        </div>
                        <div>
                          <dt>Fake värde</dt>
                          <dd>{row.fakeValue}</dd>
                        </div>
                      </dl>
                    </li>
                  ))}
                </ul>
              </article>
            ))}
          </div>
        </section>
      )}

      <div className="dashboard__list-wrap">
        <h2 className="dashboard__subtitle">Lufttemperatur per station</h2>
        {readingsMetaLine != null && (
          <p className="dashboard__table-meta">{readingsMetaLine}</p>
        )}
        {(comparison?.rows.length ?? 0) === 0 && !loading ? (
          <p className="dashboard__empty">Inga avläsningar att visa.</p>
        ) : (
          <table className="dashboard__table">
            <thead>
              <tr>
                <th scope="col" className="dashboard__table-th-ordinal" rowSpan={2}>
                  Antal
                </th>
                <th scope="col" rowSpan={2}>
                  Station
                </th>
                <th
                  scope="colgroup"
                  colSpan={2}
                  className="dashboard__table-group dashboard__table-group--first"
                >
                  Nu
                </th>
                <th
                  scope="colgroup"
                  colSpan={2}
                  className="dashboard__table-group"
                >
                  {comparison?.lastMonthHeading ?? 'Förra månaden'}
                </th>
                <th
                  scope="col"
                  rowSpan={2}
                  className="dashboard__table-group dashboard__table-th-fake"
                >
                  Test
                </th>
                <th
                  scope="col"
                  rowSpan={2}
                  className="dashboard__table-group dashboard__table-th-fake"
                >
                  Fake värde
                </th>
              </tr>
              <tr>
                <th scope="col">Tidpunkt</th>
                <th scope="col">Värde</th>
                <th scope="col">Tidpunkt</th>
                <th scope="col">Värde</th>
              </tr>
            </thead>
            <tbody>
              {(comparison?.rows ?? []).map((r, i) => {
                const lm = formatSnapshot(r.lastMonth)
                const cur = formatSnapshot(r.current)
                const fake = fakeLatestBySensor.get(r.sensorId)
                const fakeTime = fake
                  ? new Date(fake.timestamp).toLocaleString('sv-SE', {
                      dateStyle: 'short',
                      timeStyle: 'medium',
                    })
                  : '—'
                const fakeVal = fake ? `${fake.value.toFixed(1)} °C` : '—'
                return (
                  <tr key={`${r.sensorId}-${r.current.timestamp}-${i}`}>
                    <td className="dashboard__table-ordinal">{i + 1}</td>
                    <td>{r.sensorId}</td>
                    <td>{cur.time}</td>
                    <td>{cur.value}</td>
                    <td>{lm.time}</td>
                    <td>{lm.value}</td>
                    <td className="dashboard__table-fake">{fakeTime}</td>
                    <td className="dashboard__table-fake">{fakeVal}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        )}
      </div>
    </section>
  )
}
