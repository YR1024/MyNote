package com.dreamycinema.android.model

data class SessionInfo(
    val authenticated: Boolean,
    val setupRequired: Boolean,
    val canSetup: Boolean,
    val requestToken: String
)

data class TagItem(
    val id: Int,
    val categoryId: Int,
    val categoryName: String,
    val name: String,
    val videoCount: Int
)

data class TagCategory(
    val id: Int,
    val name: String,
    val tags: List<TagItem>
)

data class SubtitleTrack(
    val id: String,
    val label: String,
    val language: String,
    val kind: String,
    val source: String,
    val revisionStage: String,
    val format: String,
    val cueCount: Int,
    val isDefault: Boolean,
    val vttUrl: String
)

data class HardSubtitle(
    val subtitleTrackId: String,
    val subtitleLabel: String,
    val size: Long,
    val isStale: Boolean,
    val streamUrl: String,
    val style: HardSubtitleStyle?
)

data class MaintenanceVideo(
    val id: String,
    val number: String,
    val title: String,
    val fileName: String,
    val size: Long,
    val status: String,
    val updatedAt: String
)

data class BackupPackage(
    val id: String,
    val fileName: String,
    val createdAt: String,
    val size: Long,
    val videoCount: Long,
    val coverCount: Int,
    val subtitleFileCount: Int,
    val purpose: String,
    val isValid: Boolean,
    val validationError: String?
)

data class MediaJob(
    val id: String,
    val type: String,
    val status: String,
    val videoId: String?,
    val progress: Int,
    val stage: String,
    val currentItem: String?,
    val error: String?,
    val cancellationRequested: Boolean,
    val attemptCount: Int,
    val createdAt: String
) {
    val isActive: Boolean
        get() = status == "Queued" || status == "Running"

    val canRetry: Boolean
        get() = status == "Failed" || status == "Cancelled"
}

data class AiProviderStatus(
    val provider: String,
    val model: String,
    val available: Boolean,
    val mock: Boolean
)

data class AiSubtitleStatus(
    val enabled: Boolean,
    val targetLanguage: String,
    val speech: AiProviderStatus,
    val translation: AiProviderStatus,
    val preserveExplicitLanguage: Boolean,
    val translationStyle: String
)

data class HardSubtitleStyle(
    val fontName: String,
    val fontSize: Int,
    val textColor: String,
    val textOpacity: Int,
    val outlineColor: String,
    val outline: Int,
    val marginV: Int
)

data class VideoItem(
    val id: String,
    val number: String,
    val title: String,
    val description: String,
    val fileName: String,
    val size: Long,
    val durationSeconds: Double?,
    val width: Int?,
    val height: Int?,
    val videoCodec: String?,
    val playbackWarning: String?,
    val streamUrl: String,
    val originalAvailable: Boolean,
    val originalStreamUrl: String,
    val hardSubtitle: HardSubtitle?,
    val coverUrl: String?,
    val createdAt: String,
    val subtitles: List<SubtitleTrack>,
    val tags: List<TagItem>
)

data class VideoPage(
    val items: List<VideoItem>,
    val total: Int,
    val page: Int,
    val pageSize: Int,
    val hasMore: Boolean
)

enum class VideoSort(val queryValue: String, val displayName: String) {
    RECENT("recent", "最近同步"),
    CREATED_DESC("created-desc", "创建日期（新到旧）"),
    CREATED_ASC("created-asc", "创建日期（旧到新）"),
    NUMBER_ASC("number-asc", "番号"),
    SIZE_DESC("size-desc", "文件大小")
}
