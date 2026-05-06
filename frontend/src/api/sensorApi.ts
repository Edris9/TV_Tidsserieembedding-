import axios from 'axios';

/** I dev används Vite-proxy (vite.config) → samma ursprung, inget fel portnummer. Sätt VITE_API_BASE_URL vid prod-build om API ligger annorstädes. */
const BASE_URL = (
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ??
  (import.meta.env.DEV ? '/api/sensor' : 'http://localhost:5000/api/sensor')
);

export interface SensorReading {
  sensorId: string;
  timestamp: string;
  value: number;
  unit: string;
}

export interface ReadingSnapshot {
  timestamp: string;
  value: number;
  unit: string;
}

export interface StationComparisonRow {
  sensorId: string;
  current: ReadingSnapshot;
  lastMonth: ReadingSnapshot | null;
}

export interface ReadingsComparisonResponse {
  lastMonthHeading: string;
  rows: StationComparisonRow[];
  /** limit som skickats till Trafikverket (1–2000). */
  requestedLimit?: number;
  /** Antal WeatherMeasurepoint i senaste JSON-svaret (aktuellt läge). */
  measurepointsInResponse?: number;
  /** Poster utan komplett provtid/lufttemperatur. */
  skippedIncompleteMeasurepoints?: number;
  /** Avläsningar med komplett data innan senaste per station. */
  parsedObservationCount?: number;
}

export interface AnalysisResult {
  stationCount: number;
  embedding: number[];
  isAnomaly: boolean;
}

/** Syntetisk historik från /api/sensor/history (30 punkter per station i listan). */
export interface FakeHistoryPoint {
  sensorId: string;
  timestamp: string;
  value: number;
}

export const getReadings = async (
  stationLimit?: number,
): Promise<ReadingsComparisonResponse> => {
  const response = await axios.get(`${BASE_URL}/readings`, {
    params:
      stationLimit != null && stationLimit > 0
        ? { limit: stationLimit }
        : undefined,
  });
  return response.data;
};

export const analyzeLive = async (
  stationLimit?: number,
): Promise<AnalysisResult> => {
  const response = await axios.get(`${BASE_URL}/analyze-live`, {
    params:
      stationLimit != null && stationLimit > 0
        ? { limit: stationLimit }
        : undefined,
  });
  return response.data;
};

export const getFakeHistory = async (
  stationLimit?: number,
): Promise<FakeHistoryPoint[]> => {
  const response = await axios.get(`${BASE_URL}/history`, {
    params:
      stationLimit != null && stationLimit > 0
        ? { limit: stationLimit }
        : undefined,
  });
  return response.data;
};
