package com.dreamycinema.android.ui

import android.content.Intent
import android.content.pm.ActivityInfo
import android.graphics.Color
import android.os.Bundle
import android.view.Menu
import android.view.View
import android.view.ViewGroup
import android.view.WindowManager
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.ScrollView
import androidx.activity.OnBackPressedCallback
import androidx.annotation.OptIn
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import androidx.lifecycle.lifecycleScope
import androidx.media3.common.C
import androidx.media3.common.MediaItem
import androidx.media3.common.MimeTypes
import androidx.media3.common.PlaybackException
import androidx.media3.common.Player
import androidx.media3.common.util.UnstableApi
import androidx.media3.datasource.DefaultHttpDataSource
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.exoplayer.source.DefaultMediaSourceFactory
import androidx.media3.ui.CaptionStyleCompat
import com.dreamycinema.android.data.ApiException
import com.dreamycinema.android.data.AppServices
import com.dreamycinema.android.data.UnauthorizedException
import com.dreamycinema.android.databinding.ActivityPlayerBinding
import com.dreamycinema.android.model.AiSubtitleStatus
import com.dreamycinema.android.model.HardSubtitleStyle
import com.dreamycinema.android.model.MediaJob
import com.dreamycinema.android.model.SubtitleTrack
import com.dreamycinema.android.model.VideoItem
import com.dreamycinema.android.util.applySafeDrawingInsets
import com.dreamycinema.android.util.formatBytes
import com.dreamycinema.android.util.formatDate
import com.dreamycinema.android.util.formatDuration
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.google.android.material.snackbar.Snackbar
import kotlinx.coroutines.Job
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

@OptIn(UnstableApi::class)
class PlayerActivity : AppCompatActivity() {
    private lateinit var binding: ActivityPlayerBinding
    private lateinit var player: ExoPlayer
    private var video: VideoItem? = null
    private var sources: List<SourceOption> = emptyList()
    private var subtitles: List<SubtitleTrack?> = emptyList()
    private var settingOptions = false
    private var isFullscreen = false
    private var restoredPosition = 0L
    private var aiStatus: AiSubtitleStatus? = null
    private var hardSubtitleStyle: HardSubtitleStyle? = null
    private var translationTracks: List<SubtitleTrack> = emptyList()
    private var hardSubtitleTracks: List<SubtitleTrack> = emptyList()
    private var aiJob: MediaJob? = null
    private var hardSubtitleJob: MediaJob? = null
    private var aiPoller: Job? = null
    private var hardSubtitlePoller: Job? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityPlayerBinding.inflate(layoutInflater)
        setContentView(binding.root)
        applySafeDrawingInsets(binding.root)

        restoredPosition = savedInstanceState?.getLong(STATE_POSITION) ?: 0L
        setupPlayer()
        setupNavigation()
        setupAutomationControls()

        val videoId = intent.getStringExtra(EXTRA_VIDEO_ID)
        if (videoId.isNullOrBlank()) {
            finish()
            return
        }
        loadVideo(videoId)
    }

    private fun setupPlayer() {
        val httpFactory = DefaultHttpDataSource.Factory()
            .setUserAgent("DreamyCinemaAndroid/0.1")
            .setConnectTimeoutMs(10_000)
            .setReadTimeoutMs(30_000)
            .setAllowCrossProtocolRedirects(false)
            .setDefaultRequestProperties(AppServices.api.mediaRequestHeaders())
        val mediaSourceFactory = DefaultMediaSourceFactory(this)
            .setDataSourceFactory(httpFactory)

        player = ExoPlayer.Builder(this)
            .setMediaSourceFactory(mediaSourceFactory)
            .build()
        binding.playerView.player = player
        binding.playerView.subtitleView?.apply {
            setStyle(
                CaptionStyleCompat(
                    Color.WHITE,
                    Color.TRANSPARENT,
                    Color.TRANSPARENT,
                    CaptionStyleCompat.EDGE_TYPE_OUTLINE,
                    Color.BLACK,
                    null
                )
            )
            setFractionalTextSize(0.052f)
        }
        binding.playerView.setFullscreenButtonClickListener { fullscreen ->
            setFullscreen(fullscreen)
        }
        player.addListener(object : Player.Listener {
            override fun onIsPlayingChanged(isPlaying: Boolean) {
                if (isPlaying) {
                    window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
                } else {
                    window.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
                }
            }

            override fun onPlayerError(error: PlaybackException) {
                Snackbar.make(
                    binding.root,
                    "视频播放失败：${error.errorCodeName}",
                    Snackbar.LENGTH_LONG
                ).show()
            }
        })
    }

    private fun setupNavigation() {
        binding.toolbar.setNavigationOnClickListener { finish() }
        binding.toolbar.menu.add(Menu.NONE, MENU_MANAGE, Menu.NONE, "管理中心")
            .setShowAsAction(android.view.MenuItem.SHOW_AS_ACTION_NEVER)
        binding.toolbar.menu.add(Menu.NONE, MENU_TRASH, Menu.NONE, "移入回收站")
            .setShowAsAction(android.view.MenuItem.SHOW_AS_ACTION_NEVER)
        binding.toolbar.setOnMenuItemClickListener { item ->
            when (item.itemId) {
                MENU_MANAGE -> {
                    startActivity(Intent(this, ManagementActivity::class.java))
                    true
                }
                MENU_TRASH -> {
                    confirmTrashVideo()
                    true
                }
                else -> false
            }
        }
        onBackPressedDispatcher.addCallback(this, object : OnBackPressedCallback(true) {
            override fun handleOnBackPressed() {
                if (isFullscreen) setFullscreen(false) else finish()
            }
        })
    }

    private fun confirmTrashVideo() {
        val value = video ?: return
        MaterialAlertDialogBuilder(this)
            .setTitle("移入回收站")
            .setMessage("将“${value.number}”移入服务器回收站？之后可在管理中心恢复。")
            .setNegativeButton("取消", null)
            .setPositiveButton("移入回收站") { _, _ ->
                lifecycleScope.launch {
                    try {
                        player.pause()
                        AppServices.api.trashVideo(value.id)
                        finish()
                    } catch (exception: Exception) {
                        handleError(exception)
                    }
                }
            }
            .show()
    }

    private fun setupAutomationControls() {
        binding.recognizeButton.setOnClickListener { startRecognition() }
        binding.translateButton.setOnClickListener { startTranslation() }
        binding.cancelAiJobButton.setOnClickListener { cancelJob(aiJob, isHardSubtitle = false) }
        binding.retryAiJobButton.setOnClickListener { retryJob(aiJob, isHardSubtitle = false) }
        binding.hardSubtitleSettingsButton.setOnClickListener { showHardSubtitleStyleDialog() }
        binding.generateHardSubtitleButton.setOnClickListener { startHardSubtitle() }
        binding.deleteHardSubtitleButton.setOnClickListener { confirmDeleteHardSubtitle() }
        binding.cancelHardSubtitleJobButton.setOnClickListener {
            cancelJob(hardSubtitleJob, isHardSubtitle = true)
        }
        binding.retryHardSubtitleJobButton.setOnClickListener {
            retryJob(hardSubtitleJob, isHardSubtitle = true)
        }
        binding.deleteOriginalButton.setOnClickListener { confirmDeleteOriginal() }
    }

    private fun loadVideo(id: String) {
        binding.playerProgress.visibility = View.VISIBLE
        lifecycleScope.launch {
            try {
                val loaded = coroutineScope {
                    val videoTask = async { AppServices.api.getVideo(id) }
                    val aiStatusTask = async { AppServices.api.getAiSubtitleStatus() }
                    val hardOptionsTask = async { AppServices.api.getHardSubtitleDefaults() }
                    val jobsTask = async { AppServices.api.getJobs() }
                    PlayerData(
                        videoTask.await(),
                        aiStatusTask.await(),
                        hardOptionsTask.await(),
                        jobsTask.await()
                    )
                }
                aiStatus = loaded.aiStatus
                hardSubtitleStyle = loaded.video.hardSubtitle?.style ?: loaded.hardSubtitleDefaults
                aiJob = loaded.jobs.firstOrNull {
                    it.videoId == id &&
                        (it.type == "SpeechRecognition" || it.type == "SubtitleTranslation")
                }
                hardSubtitleJob = loaded.jobs.firstOrNull {
                    it.videoId == id && it.type == "HardSubtitle"
                }
                bindVideo(loaded.video)
                bindAutomationOptions()
                renderAutomationState()
                aiJob?.takeIf { it.isActive }?.let { pollJob(it.id, isHardSubtitle = false) }
                hardSubtitleJob?.takeIf { it.isActive }?.let {
                    pollJob(it.id, isHardSubtitle = true)
                }
            } catch (exception: UnauthorizedException) {
                AppServices.session.clearAuthentication()
                startActivity(
                    Intent(this@PlayerActivity, LoginActivity::class.java)
                        .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK)
                )
                finish()
            } catch (exception: Exception) {
                handleError(exception, returnAction = true)
            } finally {
                binding.playerProgress.visibility = View.GONE
            }
        }
    }

    private fun bindVideo(value: VideoItem) {
        video = value
        binding.toolbar.title = value.number
        binding.number.text = value.number
        binding.title.text = value.title.ifBlank { "未命名" }
        binding.description.text = value.description
        binding.tags.text = value.tags.joinToString("  ") { "#${it.name}" }
        binding.meta.text = buildString {
            append("创建日期：${formatDate(value.createdAt)}\n")
            append("文件大小：${formatBytes(value.size)}\n")
            append("视频时长：${formatDuration(value.durationSeconds)}\n")
            append("分辨率：${value.width ?: "-"}x${value.height ?: "-"}\n")
            append("视频编码：${value.videoCodec ?: "未知"}")
        }
        binding.warning.text = value.playbackWarning.orEmpty()
        binding.warning.visibility =
            if (value.playbackWarning.isNullOrBlank()) View.GONE else View.VISIBLE

        sources = buildList {
            if (value.originalAvailable) {
                add(SourceOption("原始视频", value.originalStreamUrl))
            }
            value.hardSubtitle?.let {
                val stale = if (it.isStale) "（字幕已更新，建议重新生成）" else ""
                add(SourceOption("硬字幕版$stale", it.streamUrl))
            }
        }
        subtitles = listOf(null) + value.subtitles

        settingOptions = true
        binding.sourceSpinner.adapter = ArrayAdapter(
            this,
            android.R.layout.simple_spinner_dropdown_item,
            sources.map { it.label }
        )
        binding.subtitleSpinner.adapter = ArrayAdapter(
            this,
            android.R.layout.simple_spinner_dropdown_item,
            subtitles.map { track -> track?.let { "字幕：${it.label}" } ?: "字幕：关闭" }
        )
        val defaultSubtitle = value.subtitles.indexOfFirst { it.isDefault }
        binding.subtitleSpinner.setSelection(if (defaultSubtitle >= 0) defaultSubtitle + 1 else 0)
        settingOptions = false

        binding.sourceSpinner.onItemSelectedListener = selectionListener
        binding.subtitleSpinner.onItemSelectedListener = selectionListener
        if (sources.isEmpty()) {
            Snackbar.make(binding.root, "该视频当前没有可播放文件。", Snackbar.LENGTH_INDEFINITE)
                .show()
        } else {
            prepareSelectedMedia(restoredPosition)
            restoredPosition = 0L
        }
    }

    private fun bindAutomationOptions() {
        val value = video ?: return
        translationTracks = value.subtitles.filter {
            it.cueCount > 0 && it.kind != "Translated" && !it.language.lowercase().startsWith("zh")
        }
        hardSubtitleTracks = value.subtitles.filter { it.cueCount > 0 }

        binding.translationTrackSpinner.adapter = ArrayAdapter(
            this,
            android.R.layout.simple_spinner_dropdown_item,
            translationTracks.map { "原文：${it.label} · ${it.cueCount} 条" }
                .ifEmpty { listOf("没有可翻译的原文字幕") }
        )
        binding.hardSubtitleTrackSpinner.adapter = ArrayAdapter(
            this,
            android.R.layout.simple_spinner_dropdown_item,
            hardSubtitleTracks.map { "${it.label} · ${it.cueCount} 条" }
                .ifEmpty { listOf("没有可烧录的字幕") }
        )

        val preferredHardTrack = value.hardSubtitle?.subtitleTrackId
            ?: hardSubtitleTracks.firstOrNull {
                it.revisionStage == "FinalPolished" &&
                    (it.language.lowercase().startsWith("zh") || it.kind == "Bilingual")
            }?.id
            ?: hardSubtitleTracks.firstOrNull { it.language.lowercase().startsWith("zh") }?.id
            ?: hardSubtitleTracks.firstOrNull()?.id
        val preferredIndex = hardSubtitleTracks.indexOfFirst { it.id == preferredHardTrack }
        if (preferredIndex >= 0) binding.hardSubtitleTrackSpinner.setSelection(preferredIndex)
    }

    private fun renderAutomationState() {
        val value = video ?: return
        val status = aiStatus
        binding.aiStatus.text = if (status == null) {
            "无法读取 AI Provider 状态。"
        } else {
            val speech = providerLabel(status.speech.provider, status.speech.model, status.speech.mock)
            val translation = providerLabel(
                status.translation.provider,
                status.translation.model,
                status.translation.mock
            )
            "识别：$speech\n翻译：$translation · 目标 ${status.targetLanguage}"
        }

        val aiActive = aiJob?.isActive == true
        binding.recognizeButton.isEnabled = status?.enabled == true &&
            status.speech.available && !aiActive && value.originalAvailable
        binding.translateButton.isEnabled = status?.enabled == true &&
            status.translation.available && translationTracks.isNotEmpty() && !aiActive
        binding.translationTrackSpinner.isEnabled = translationTracks.isNotEmpty() && !aiActive
        renderJob(
            aiJob,
            binding.aiJobStatus,
            binding.aiJobProgress,
            binding.cancelAiJobButton,
            binding.retryAiJobButton
        )

        val hard = value.hardSubtitle
        binding.hardSubtitleStatus.text = when {
            hard == null -> "尚未生成。字幕将写入画面，适合手机全屏播放。"
            hard.isStale -> "已有 ${hard.subtitleLabel} 版本，但字幕已修改，需要重新生成。"
            else -> "已生成：${hard.subtitleLabel} · ${formatBytes(hard.size)}"
        }
        binding.hardSubtitleStyleSummary.text = hardSubtitleStyle?.let {
            "${it.fontName} · ${it.fontSize}px · 不透明度 ${it.textOpacity}%"
        }.orEmpty()

        val hardActive = hardSubtitleJob?.isActive == true
        binding.hardSubtitleTrackSpinner.isEnabled =
            hardSubtitleTracks.isNotEmpty() && !hardActive && value.originalAvailable
        binding.hardSubtitleSettingsButton.isEnabled = !hardActive
        binding.generateHardSubtitleButton.isEnabled =
            value.originalAvailable && hardSubtitleTracks.isNotEmpty() && !hardActive
        binding.generateHardSubtitleButton.text =
            if (hard == null) "生成硬字幕版" else "重新生成硬字幕版"
        binding.deleteHardSubtitleButton.visibility =
            if (hard != null && value.originalAvailable) View.VISIBLE else View.GONE
        binding.deleteHardSubtitleButton.isEnabled = !hardActive
        binding.deleteOriginalButton.visibility =
            if (hard != null && value.originalAvailable) View.VISIBLE else View.GONE
        binding.deleteOriginalButton.isEnabled = !hardActive && hard?.isStale == false
        renderJob(
            hardSubtitleJob,
            binding.hardSubtitleJobStatus,
            binding.hardSubtitleJobProgress,
            binding.cancelHardSubtitleJobButton,
            binding.retryHardSubtitleJobButton
        )
    }

    private fun renderJob(
        job: MediaJob?,
        statusView: android.widget.TextView,
        progressView: android.widget.ProgressBar,
        cancelButton: View,
        retryButton: View
    ) {
        val visible = job != null
        statusView.visibility = if (visible) View.VISIBLE else View.GONE
        progressView.visibility = if (visible) View.VISIBLE else View.GONE
        if (job != null) {
            statusView.text = buildString {
                append("${jobStatusLabel(job.status)} · ${job.stage} · ${job.progress}%")
                job.currentItem?.takeIf { it.isNotBlank() }?.let { append("\n$it") }
                job.error?.takeIf { it.isNotBlank() }?.let { append("\n$it") }
            }
            progressView.progress = job.progress
        }
        cancelButton.visibility = if (job?.isActive == true) View.VISIBLE else View.GONE
        retryButton.visibility = if (job?.canRetry == true) View.VISIBLE else View.GONE
    }

    private fun startRecognition() {
        val value = video ?: return
        lifecycleScope.launch {
            try {
                aiJob = AppServices.api.startSpeechRecognition(value.id)
                renderAutomationState()
                pollJob(aiJob!!.id, isHardSubtitle = false)
            } catch (exception: Exception) {
                handleError(exception)
            }
        }
    }

    private fun startTranslation() {
        val value = video ?: return
        val track = translationTracks.getOrNull(binding.translationTrackSpinner.selectedItemPosition)
            ?: return
        lifecycleScope.launch {
            try {
                val force = value.subtitles.any { it.kind == "Translated" }
                aiJob = AppServices.api.startSubtitleTranslation(value.id, track.id, force)
                renderAutomationState()
                pollJob(aiJob!!.id, isHardSubtitle = false)
            } catch (exception: Exception) {
                handleError(exception)
            }
        }
    }

    private fun startHardSubtitle() {
        val value = video ?: return
        val track = hardSubtitleTracks.getOrNull(
            binding.hardSubtitleTrackSpinner.selectedItemPosition
        ) ?: return
        val style = hardSubtitleStyle ?: return
        lifecycleScope.launch {
            try {
                hardSubtitleJob = AppServices.api.startHardSubtitle(value.id, track.id, style)
                renderAutomationState()
                pollJob(hardSubtitleJob!!.id, isHardSubtitle = true)
            } catch (exception: Exception) {
                handleError(exception)
            }
        }
    }

    private fun pollJob(id: String, isHardSubtitle: Boolean) {
        val poller = lifecycleScope.launch {
            while (true) {
                delay(1_000)
                try {
                    val latest = AppServices.api.getJob(id)
                    if (isHardSubtitle) hardSubtitleJob = latest else aiJob = latest
                    renderAutomationState()
                    if (!latest.isActive) {
                        if (latest.status == "Completed") {
                            restoredPosition = player.currentPosition
                            video?.id?.let(::loadVideo)
                        }
                        break
                    }
                } catch (exception: Exception) {
                    handleError(exception)
                    break
                }
            }
        }
        if (isHardSubtitle) {
            hardSubtitlePoller?.cancel()
            hardSubtitlePoller = poller
        } else {
            aiPoller?.cancel()
            aiPoller = poller
        }
    }

    private fun cancelJob(job: MediaJob?, isHardSubtitle: Boolean) {
        if (job?.isActive != true) return
        lifecycleScope.launch {
            try {
                val updated = AppServices.api.cancelJob(job.id)
                if (isHardSubtitle) hardSubtitleJob = updated else aiJob = updated
                renderAutomationState()
            } catch (exception: Exception) {
                handleError(exception)
            }
        }
    }

    private fun retryJob(job: MediaJob?, isHardSubtitle: Boolean) {
        if (job?.canRetry != true) return
        lifecycleScope.launch {
            try {
                val updated = AppServices.api.retryJob(job.id)
                if (isHardSubtitle) hardSubtitleJob = updated else aiJob = updated
                renderAutomationState()
                pollJob(updated.id, isHardSubtitle)
            } catch (exception: Exception) {
                handleError(exception)
            }
        }
    }

    private fun confirmDeleteHardSubtitle() {
        val value = video ?: return
        MaterialAlertDialogBuilder(this)
            .setTitle("删除硬字幕版")
            .setMessage("删除硬字幕文件并保留原视频？需要重新执行 FFmpeg 才能恢复。")
            .setNegativeButton("取消", null)
            .setPositiveButton("删除") { _, _ ->
                lifecycleScope.launch {
                    try {
                        AppServices.api.deleteHardSubtitle(value.id)
                        restoredPosition = player.currentPosition
                        loadVideo(value.id)
                    } catch (exception: Exception) {
                        handleError(exception)
                    }
                }
            }
            .show()
    }

    private fun confirmDeleteOriginal() {
        val value = video ?: return
        val input = EditText(this).apply {
            hint = "输入番号 ${value.number}"
            setSingleLine()
            setTextColor(getColor(com.dreamycinema.android.R.color.dc_text))
        }
        val wrapper = LinearLayout(this).apply {
            setPadding(24.dp, 0, 24.dp, 0)
            addView(input, ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT)
        }
        val dialog = MaterialAlertDialogBuilder(this)
            .setTitle("永久删除原视频")
            .setMessage("删除后只能播放现有硬字幕版，无法更换字幕或重新生成。请输入番号确认。")
            .setView(wrapper)
            .setNegativeButton("取消", null)
            .setPositiveButton("永久删除", null)
            .create()
        dialog.setOnShowListener {
            dialog.getButton(android.app.AlertDialog.BUTTON_POSITIVE).setOnClickListener {
                if (input.text.toString().trim() != value.number) {
                    input.error = "番号不匹配"
                    return@setOnClickListener
                }
                dialog.dismiss()
                lifecycleScope.launch {
                    try {
                        AppServices.api.deleteOriginal(value.id, value.number)
                        restoredPosition = player.currentPosition
                        loadVideo(value.id)
                    } catch (exception: Exception) {
                        handleError(exception)
                    }
                }
            }
        }
        dialog.show()
    }

    private fun showHardSubtitleStyleDialog() {
        val current = hardSubtitleStyle ?: return
        val fields = listOf(
            styleField("字体名称", current.fontName),
            styleField("字体大小（12-48）", current.fontSize.toString()),
            styleField("文字颜色（#RRGGBB）", current.textColor),
            styleField("不透明度（20-100）", current.textOpacity.toString()),
            styleField("描边颜色（#RRGGBB）", current.outlineColor),
            styleField("描边宽度（0-6）", current.outline.toString()),
            styleField("底部间距（0-160）", current.marginV.toString())
        )
        val content = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(22.dp, 0, 22.dp, 8.dp)
            fields.forEach { addView(it) }
        }
        val scroll = ScrollView(this).apply { addView(content) }
        val dialog = MaterialAlertDialogBuilder(this)
            .setTitle("硬字幕样式")
            .setView(scroll)
            .setNegativeButton("取消", null)
            .setPositiveButton("应用", null)
            .create()
        dialog.setOnShowListener {
            dialog.getButton(android.app.AlertDialog.BUTTON_POSITIVE).setOnClickListener {
                try {
                    val next = HardSubtitleStyle(
                        fontName = fields[0].text.toString().trim().also {
                            require(it.isNotBlank() && ',' !in it && '\'' !in it) { "字体名称无效" }
                        },
                        fontSize = fields[1].intValue(12..48, "字体大小"),
                        textColor = fields[2].colorValue("文字颜色"),
                        textOpacity = fields[3].intValue(20..100, "不透明度"),
                        outlineColor = fields[4].colorValue("描边颜色"),
                        outline = fields[5].intValue(0..6, "描边宽度"),
                        marginV = fields[6].intValue(0..160, "底部间距")
                    )
                    hardSubtitleStyle = next
                    renderAutomationState()
                    dialog.dismiss()
                } catch (exception: IllegalArgumentException) {
                    fields[0].error = exception.message
                }
            }
        }
        dialog.show()
    }

    private fun styleField(label: String, value: String) = EditText(this).apply {
        hint = label
        setText(value)
        setTextColor(getColor(com.dreamycinema.android.R.color.dc_text))
        setHintTextColor(getColor(com.dreamycinema.android.R.color.dc_text_muted))
        setSingleLine()
    }

    private fun EditText.intValue(range: IntRange, label: String): Int {
        val number = text.toString().trim().toIntOrNull()
            ?: throw IllegalArgumentException("$label 必须是数字")
        require(number in range) { "$label 必须在 ${range.first}-${range.last} 之间" }
        return number
    }

    private fun EditText.colorValue(label: String): String {
        val value = text.toString().trim().uppercase()
        require(Regex("^#[0-9A-F]{6}$").matches(value)) { "$label 必须是 #RRGGBB" }
        return value
    }

    private fun handleError(exception: Exception, returnAction: Boolean = false) {
        if (exception is UnauthorizedException) {
            AppServices.session.clearAuthentication()
            startActivity(
                Intent(this, LoginActivity::class.java)
                    .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK)
            )
            finish()
            return
        }
        val bar = Snackbar.make(
            binding.root,
            (exception as? ApiException)?.message ?: exception.message ?: "操作失败。",
            if (returnAction) Snackbar.LENGTH_INDEFINITE else Snackbar.LENGTH_LONG
        )
        if (returnAction) bar.setAction("返回") { finish() }
        bar.show()
    }

    private fun providerLabel(provider: String, model: String, mock: Boolean): String =
        buildString {
            append(provider)
            if (model.isNotBlank()) append(" · $model")
            if (mock) append("（模拟）")
        }

    private fun jobStatusLabel(status: String) = when (status) {
        "Queued" -> "等待中"
        "Running" -> "执行中"
        "Completed" -> "已完成"
        "Failed" -> "失败"
        "Cancelled" -> "已取消"
        else -> status
    }

    private val selectionListener = object : AdapterView.OnItemSelectedListener {
        override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
            if (!settingOptions && sources.isNotEmpty()) {
                prepareSelectedMedia(player.currentPosition)
            }
        }

        override fun onNothingSelected(parent: AdapterView<*>?) = Unit
    }

    private fun prepareSelectedMedia(position: Long) {
        val source = sources.getOrNull(binding.sourceSpinner.selectedItemPosition) ?: return
        val subtitle = subtitles.getOrNull(binding.subtitleSpinner.selectedItemPosition)
        val subtitleConfigurations = subtitle?.let {
            listOf(
                MediaItem.SubtitleConfiguration.Builder(
                    android.net.Uri.parse(AppServices.api.absoluteUrl(it.vttUrl))
                )
                    .setMimeType(MimeTypes.TEXT_VTT)
                    .setLanguage(it.language.ifBlank { "und" })
                    .setLabel(it.label)
                    .setSelectionFlags(C.SELECTION_FLAG_DEFAULT)
                    .build()
            )
        }.orEmpty()

        val wasPlaying = player.playWhenReady
        val mediaItem = MediaItem.Builder()
            .setUri(AppServices.api.absoluteUrl(source.url))
            .setSubtitleConfigurations(subtitleConfigurations)
            .build()
        player.setMediaItem(mediaItem, position.coerceAtLeast(0L))
        player.prepare()
        player.playWhenReady = wasPlaying
    }

    private fun setFullscreen(fullscreen: Boolean) {
        isFullscreen = fullscreen
        binding.toolbar.visibility = if (fullscreen) View.GONE else View.VISIBLE
        binding.sourceControls.visibility = if (fullscreen) View.GONE else View.VISIBLE
        binding.detailsScroll.visibility = if (fullscreen) View.GONE else View.VISIBLE
        binding.playerStage.layoutParams = binding.playerStage.layoutParams.apply {
            height = if (fullscreen) ViewGroup.LayoutParams.MATCH_PARENT else 230.dp
        }

        val insets = WindowInsetsControllerCompat(window, window.decorView)
        if (fullscreen) {
            requestedOrientation = ActivityInfo.SCREEN_ORIENTATION_SENSOR_LANDSCAPE
            WindowCompat.setDecorFitsSystemWindows(window, false)
            insets.systemBarsBehavior =
                WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
            insets.hide(WindowInsetsCompat.Type.systemBars())
        } else {
            requestedOrientation = ActivityInfo.SCREEN_ORIENTATION_UNSPECIFIED
            WindowCompat.setDecorFitsSystemWindows(window, false)
            insets.show(WindowInsetsCompat.Type.systemBars())
        }
    }

    override fun onSaveInstanceState(outState: Bundle) {
        outState.putLong(STATE_POSITION, player.currentPosition)
        super.onSaveInstanceState(outState)
    }

    override fun onStop() {
        super.onStop()
        if (!isChangingConfigurations) player.pause()
    }

    override fun onDestroy() {
        aiPoller?.cancel()
        hardSubtitlePoller?.cancel()
        binding.playerView.player = null
        player.release()
        super.onDestroy()
    }

    private val Int.dp: Int
        get() = (this * resources.displayMetrics.density).toInt()

    private data class SourceOption(val label: String, val url: String)

    private data class PlayerData(
        val video: VideoItem,
        val aiStatus: AiSubtitleStatus,
        val hardSubtitleDefaults: HardSubtitleStyle,
        val jobs: List<MediaJob>
    )

    companion object {
        const val EXTRA_VIDEO_ID = "video_id"
        private const val STATE_POSITION = "player_position"
        private const val MENU_MANAGE = 1
        private const val MENU_TRASH = 2
    }
}
