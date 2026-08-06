export interface SessionInfo {
  authenticated: boolean;
  setupRequired: boolean;
  canSetup: boolean;
  requestToken: string;
  sessionExpiresAt: string | null;
  passwordUpdatedAt: string | null;
}

export interface SecurityAuditEntry {
  id: string;
  createdAt: string;
  action: string;
  method: string;
  path: string;
  statusCode: number;
  outcome: "Succeeded" | "Denied" | "Rejected" | "Failed";
  remoteAddress: string | null;
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

export interface SubtitleReviewTrack {
  id: string;
  label: string;
  language: string;
  kind: string;
  revisionStage: SubtitleTrack["revisionStage"];
  cueCount: number;
  vttUrl: string;
}

export interface SubtitleQualityWarning {
  code: "MissingTranslation" | "TimingMismatch" | "LongText" | "FastReading";
  message: string;
}

export interface SubtitleReviewCue {
  index: number;
  sourceCueId: number;
  targetCueId: number | null;
  startMilliseconds: number;
  endMilliseconds: number;
  sourceText: string;
  targetText: string;
  isSourceManuallyEdited: boolean;
  isManuallyEdited: boolean;
  warnings: SubtitleQualityWarning[];
}

export interface SubtitleReviewDocument {
  videoId: string;
  videoNumber: string;
  streamUrl: string;
  sourceTracks: SubtitleReviewTrack[];
  targetTracks: SubtitleReviewTrack[];
  sourceTrackId: string | null;
  targetTrackId: string | null;
  cues: SubtitleReviewCue[];
  warningCount: number;
  sourceManualEditCount: number;
  manualEditCount: number;
}

export interface SubtitleReviewEditResult {
  track: SubtitleReviewTrack;
  cueId: number;
  index: number;
  text: string;
  isManuallyEdited: boolean;
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
  playbackWarning: string | null;
  streamUrl: string;
  originalAvailable: boolean;
  originalDeletedAt: string | null;
  originalStreamUrl: string;
  hardSubtitle: HardSubtitleItem | null;
  coverUrl: string | null;
  createdAt: string;
  importedAt: string;
  subtitles: SubtitleTrack[];
  tags: TagItem[];
}

export interface HardSubtitleItem {
  subtitleTrackId: string;
  subtitleLabel: string;
  size: number;
  createdAt: string;
  isStale: boolean;
  streamUrl: string;
  style: HardSubtitleStyle | null;
}

export interface HardSubtitleStyle {
  fontName: string;
  fontSize: number;
  textColor: string;
  textOpacity: number;
  outlineColor: string;
  outline: number;
  marginV: number;
}

export interface HardSubtitleOptionsInfo {
  defaultStyle: HardSubtitleStyle;
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

export interface BackupPackage {
  id: string;
  fileName: string;
  createdAt: string;
  size: number;
  sha256: string;
  formatVersion: number;
  videoCount: number;
  coverCount: number;
  subtitleFileCount: number;
  includesOriginalVideos: boolean;
  warnings: string[];
  purpose: "Manual" | "PreRestore" | "Imported" | "Unknown";
  isValid: boolean;
  validationError: string | null;
}

export interface RestoreResult {
  backupId: string;
  safetyBackup: BackupPackage;
  database: {
    videoCount: number;
    tagCategoryCount: number;
    tagCount: number;
    subtitleTrackCount: number;
    subtitleCueCount: number;
  };
  restoredCoverCount: number;
  restoredSubtitleFileCount: number;
  missingVideoCount: number;
  warnings: string[];
  configurationRestartRequired: boolean;
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

export interface MediaMaintenanceIssue {
  videoId: string;
  fileName: string | null;
  reason: string;
}

export interface MediaMaintenanceResult {
  action: "Reanalyze" | "RegenerateCover";
  requestedCount: number;
  processedCount: number;
  updatedCount: number;
  issues: MediaMaintenanceIssue[];
}

export interface HardSubtitleJobResult {
  videoId: string;
  subtitleTrackId: string;
  subtitleLabel: string;
  relativePath: string;
  size: number;
  createdAt: string;
  encoder: string;
  style: HardSubtitleStyle;
}

export interface BatchTagResult {
  videoCount: number;
  addedRelationshipCount: number;
  removedRelationshipCount: number;
}

export interface BatchTrashResult {
  videoCount: number;
  movedFileCount: number;
}

export interface ResolutionSuggestionGroup {
  tagId: number;
  tagName: string;
  count: number;
  videoIds: string[];
}

export interface ResolutionSuggestionResult {
  videoCount: number;
  noSuggestionCount: number;
  groups: ResolutionSuggestionGroup[];
}

export interface ResolutionApplyResult extends ResolutionSuggestionResult {
  updatedVideoCount: number;
  addedRelationshipCount: number;
  removedRelationshipCount: number;
}

export interface MediaJob {
  id: string;
  type: "Sync" | "SpeechRecognition" | "SubtitleTranslation" | "MediaMaintenance" | "HardSubtitle";
  status: MediaJobStatus;
  videoId: string | null;
  progress: number;
  stage: string;
  currentItem: string | null;
  error: string | null;
  result: SyncResult | AiSubtitleJobResult | MediaMaintenanceResult | HardSubtitleJobResult | null;
  cancellationRequested: boolean;
  attemptCount: number;
  createdAt: string;
  updatedAt: string;
  startedAt: string | null;
  completedAt: string | null;
}
