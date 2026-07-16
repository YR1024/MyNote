export interface SessionInfo {
  authenticated: boolean;
  setupRequired: boolean;
  canSetup: boolean;
  requestToken: string;
}

export interface TagItem {
  id: number;
  categoryId: number;
  categoryName: string;
  name: string;
  videoCount: number;
}

export interface TagCategory {
  id: number;
  name: string;
  tags: TagItem[];
}

export interface SubtitleTrack {
  id: string;
  label: string;
  language: string;
  kind: string;
  source: string;
  revisionStage: "SourceOriginal" | "RawRecognition" | "SourceCorrected" | "ChineseDraft" | "FinalPolished";
  format: string;
  cueCount: number;
  isDefault: boolean;
  vttUrl: string;
}

export interface VideoItem {
  id: string;
  number: string;
  title: string;
  description: string;
  fileName: string;
  size: number;
  durationSeconds: number | null;
  width: number | null;
  height: number | null;
  videoCodec: string | null;
  streamUrl: string;
  coverUrl: string | null;
  createdAt: string;
  importedAt: string;
  subtitles: SubtitleTrack[];
  tags: TagItem[];
}

export interface VideoPage {
  items: VideoItem[];
  total: number;
  page: number;
  pageSize: number;
  hasMore: boolean;
}

export interface MaintenanceVideo {
  id: string;
  number: string;
  title: string;
  fileName: string;
  size: number;
  status: "Trashed" | "Missing";
  coverUrl: string | null;
  updatedAt: string;
}

export interface SyncResult {
  importedCount: number;
  availableCount: number;
  missingCount: number;
  analyzedCount: number;
  generatedCoverCount: number;
  importedSubtitleCount: number;
  failed: Array<{ fileName: string; reason: string }>;
  warnings: Array<{ fileName: string; reason: string }>;
  ImportedCount?: number;
  AvailableCount?: number;
  MissingCount?: number;
  AnalyzedCount?: number;
  GeneratedCoverCount?: number;
  ImportedSubtitleCount?: number;
  Failed?: Array<{ fileName: string; reason: string }>;
  Warnings?: Array<{ fileName: string; reason: string }>;
}

export type MediaJobStatus = "Queued" | "Running" | "Completed" | "Failed" | "Cancelled";

export interface AiProviderStatus {
  provider: string;
  model: string;
  available: boolean;
  mock: boolean;
}

export interface AiSubtitleStatus {
  enabled: boolean;
  targetLanguage: string;
  speech: AiProviderStatus;
  translation: AiProviderStatus;
  preserveExplicitLanguage: boolean;
  translationStyle: string;
}

export interface AiSubtitleJobResult {
  trackId: string;
  videoId: string;
  language: string;
  cueCount: number;
  provider: string;
  model: string;
  mock: boolean;
  translated: boolean;
}

export interface MediaJob {
  id: string;
  type: "Sync" | "SpeechRecognition" | "SubtitleTranslation";
  status: MediaJobStatus;
  videoId: string | null;
  progress: number;
  stage: string;
  currentItem: string | null;
  error: string | null;
  result: SyncResult | AiSubtitleJobResult | null;
  cancellationRequested: boolean;
  attemptCount: number;
  createdAt: string;
  updatedAt: string;
  startedAt: string | null;
  completedAt: string | null;
}
