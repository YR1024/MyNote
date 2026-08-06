package com.dreamycinema.android.ui

import android.content.Intent
import android.graphics.Typeface
import android.os.Bundle
import android.provider.OpenableColumns
import android.view.Gravity
import android.view.View
import android.view.ViewGroup
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.TextView
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.dreamycinema.android.R
import com.dreamycinema.android.data.ApiException
import com.dreamycinema.android.data.AppServices
import com.dreamycinema.android.data.UnauthorizedException
import com.dreamycinema.android.databinding.ActivityManagementBinding
import com.dreamycinema.android.model.BackupPackage
import com.dreamycinema.android.model.MaintenanceVideo
import com.dreamycinema.android.model.MediaJob
import com.dreamycinema.android.model.TagCategory
import com.dreamycinema.android.util.applySafeDrawingInsets
import com.dreamycinema.android.util.formatBytes
import com.dreamycinema.android.util.formatDate
import com.google.android.material.button.MaterialButton
import com.google.android.material.card.MaterialCardView
import com.google.android.material.chip.Chip
import com.google.android.material.chip.ChipGroup
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.google.android.material.snackbar.Snackbar
import kotlinx.coroutines.Job
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

class ManagementActivity : AppCompatActivity() {
    private lateinit var binding: ActivityManagementBinding
    private var syncJob: MediaJob? = null
    private var syncPoller: Job? = null
    private var pendingDownload: BackupPackage? = null
    private var loading = false

    private val importBackupLauncher = registerForActivityResult(
        ActivityResultContracts.OpenDocument()
    ) { uri ->
        if (uri == null) return@registerForActivityResult
        lifecycleScope.launch {
            setBusy(true)
            try {
                var name = "dreamy-cinema-import.zip"
                var size = -1L
                contentResolver.query(uri, null, null, null, null)?.use { cursor ->
                    if (cursor.moveToFirst()) {
                        cursor.getColumnIndex(OpenableColumns.DISPLAY_NAME)
                            .takeIf { it >= 0 }
                            ?.let { name = cursor.getString(it) ?: name }
                        cursor.getColumnIndex(OpenableColumns.SIZE)
                            .takeIf { it >= 0 }
                            ?.let { size = cursor.getLong(it) }
                    }
                }
                val input = contentResolver.openInputStream(uri)
                    ?: error("无法读取所选文件。")
                AppServices.api.importBackup(
                    fileName = name,
                    contentType = contentResolver.getType(uri) ?: "application/zip",
                    length = size,
                    input = input
                )
                showMessage("备份包已导入，请确认内容后再执行恢复。")
                loadAll()
            } catch (exception: Exception) {
                handleError(exception)
            } finally {
                setBusy(false)
            }
        }
    }

    private val exportBackupLauncher = registerForActivityResult(
        ActivityResultContracts.CreateDocument("application/zip")
    ) { uri ->
        val backup = pendingDownload
        pendingDownload = null
        if (uri == null || backup == null) return@registerForActivityResult
        lifecycleScope.launch {
            setBusy(true)
            try {
                val output = contentResolver.openOutputStream(uri)
                    ?: error("无法写入所选位置。")
                AppServices.api.downloadBackup(backup.id, output)
                showMessage("备份已保存：${backup.fileName}")
            } catch (exception: Exception) {
                handleError(exception)
            } finally {
                setBusy(false)
            }
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityManagementBinding.inflate(layoutInflater)
        setContentView(binding.root)
        applySafeDrawingInsets(binding.root)

        binding.toolbar.setNavigationOnClickListener { finish() }
        binding.startSyncButton.setOnClickListener { startSync() }
        binding.cancelSyncButton.setOnClickListener { cancelSync() }
        binding.retrySyncButton.setOnClickListener { retrySync() }
        binding.addCategoryButton.setOnClickListener {
            showNameDialog("新增标签分类") { name ->
                performAndReload("分类已新增。") { AppServices.api.createTagCategory(name) }
            }
        }
        binding.refreshMaintenanceButton.setOnClickListener { loadAll() }
        binding.createBackupButton.setOnClickListener { createBackup() }
        binding.importBackupButton.setOnClickListener {
            importBackupLauncher.launch(arrayOf("application/zip", "application/octet-stream"))
        }
        loadAll()
    }

    override fun onDestroy() {
        syncPoller?.cancel()
        super.onDestroy()
    }

    private fun loadAll() {
        if (loading) return
        loading = true
        setBusy(true)
        lifecycleScope.launch {
            try {
                val result = coroutineScope {
                    val categories = async { AppServices.api.getTagCategories() }
                    val maintenance = async { AppServices.api.getMaintenanceVideos() }
                    val backups = async { AppServices.api.getBackups() }
                    val jobs = async { AppServices.api.getJobs() }
                    LoadedData(
                        categories.await(),
                        maintenance.await(),
                        backups.await(),
                        jobs.await()
                    )
                }
                renderCategories(result.categories)
                renderMaintenance(result.maintenance)
                renderBackups(result.backups)
                syncJob = result.jobs.firstOrNull { it.type == "Sync" }
                renderSyncJob()
                syncJob?.takeIf { it.isActive }?.let { pollSync(it.id) }
            } catch (exception: Exception) {
                handleError(exception)
            } finally {
                loading = false
                setBusy(false)
            }
        }
    }

    private fun renderCategories(categories: List<TagCategory>) {
        binding.tagsContainer.removeAllViews()
        if (categories.isEmpty()) {
            binding.tagsContainer.addView(emptyText("暂无标签分类"))
            return
        }
        categories.forEach { category ->
            val content = verticalContainer(14)
            val heading = LinearLayout(this).apply {
                gravity = Gravity.CENTER_VERTICAL
                orientation = LinearLayout.HORIZONTAL
            }
            heading.addView(titleText(category.name), weighted())
            heading.addView(actionButton("重命名") {
                showNameDialog("重命名分类", category.name) { name ->
                    performAndReload("分类已重命名。") {
                        AppServices.api.renameTagCategory(category.id, name)
                    }
                }
            })
            heading.addView(actionButton("删除", danger = true) {
                confirm("删除分类", "将同时删除分类“${category.name}”及其标签关系，确定继续？") {
                    performAndReload("分类已删除。") {
                        AppServices.api.deleteTagCategory(category.id)
                    }
                }
            })
            content.addView(heading)

            val chips = ChipGroup(this).apply {
                setPadding(0, 8.dp, 0, 2.dp)
                isSingleLine = false
            }
            category.tags.forEach { tag ->
                chips.addView(Chip(this).apply {
                    text = "#${tag.name}  ${tag.videoCount}"
                    isCloseIconVisible = true
                    setOnClickListener {
                        showNameDialog("重命名标签", tag.name) { name ->
                            performAndReload("标签已重命名。") {
                                AppServices.api.renameTag(tag.id, name)
                            }
                        }
                    }
                    setOnCloseIconClickListener {
                        confirm("删除标签", "删除标签 #${tag.name} 及全部视频关系？") {
                            performAndReload("标签已删除。") {
                                AppServices.api.deleteTag(tag.id)
                            }
                        }
                    }
                })
            }
            content.addView(chips)
            content.addView(actionButton("新增标签") {
                showNameDialog("在“${category.name}”中新建标签") { name ->
                    performAndReload("标签已新增。") {
                        AppServices.api.createTag(category.id, name)
                    }
                }
            })
            binding.tagsContainer.addView(card(content))
        }
    }

    private fun renderMaintenance(videos: List<MaintenanceVideo>) {
        binding.maintenanceContainer.removeAllViews()
        if (videos.isEmpty()) {
            binding.maintenanceContainer.addView(emptyText("回收站为空，也没有缺失文件。"))
            return
        }
        videos.forEach { video ->
            val content = verticalContainer(14)
            content.addView(titleText("${video.number} · ${statusLabel(video.status)}"))
            content.addView(bodyText(video.title.ifBlank { video.fileName }))
            content.addView(bodyText("${formatBytes(video.size)} · ${formatDate(video.updatedAt)}"))
            val actions = horizontalActions()
            if (video.status == "Trashed") {
                actions.addView(actionButton("恢复") {
                    performAndReload("视频已恢复。") { AppServices.api.restoreVideo(video.id) }
                })
            }
            actions.addView(actionButton("永久删除", danger = true) {
                confirm(
                    "永久删除视频",
                    "将永久删除“${video.number}”的文件、封面、字幕和数据库记录，无法撤销。"
                ) {
                    performAndReload("视频已永久删除。") {
                        AppServices.api.permanentlyDeleteVideo(video.id)
                    }
                }
            })
            content.addView(actions)
            binding.maintenanceContainer.addView(card(content))
        }
    }

    private fun renderBackups(backups: List<BackupPackage>) {
        binding.backupsContainer.removeAllViews()
        if (backups.isEmpty()) {
            binding.backupsContainer.addView(emptyText("暂无数据库备份。"))
            return
        }
        backups.forEach { backup ->
            val content = verticalContainer(14)
            content.addView(titleText(backup.fileName))
            content.addView(
                bodyText(
                    "${formatDate(backup.createdAt)} · ${formatBytes(backup.size)} · ${backup.videoCount} 个视频"
                )
            )
            if (!backup.isValid) {
                content.addView(errorText(backup.validationError ?: "备份包无效"))
            }
            val actions = horizontalActions()
            actions.addView(actionButton("下载") {
                pendingDownload = backup
                exportBackupLauncher.launch(backup.fileName)
            })
            actions.addView(actionButton("恢复") {
                confirm(
                    "恢复数据库备份",
                    "恢复“${backup.fileName}”会替换当前数据库、封面、字幕和可移植设置。服务器会先自动建立安全备份。"
                ) {
                    performAndReload("备份恢复完成。") {
                        AppServices.api.restoreBackup(backup.id)
                    }
                }
            }.apply { isEnabled = backup.isValid })
            actions.addView(actionButton("删除", danger = true) {
                confirm("删除备份", "删除备份文件“${backup.fileName}”？") {
                    performAndReload("备份已删除。") { AppServices.api.deleteBackup(backup.id) }
                }
            })
            content.addView(actions)
            binding.backupsContainer.addView(card(content))
        }
    }

    private fun startSync() {
        lifecycleScope.launch {
            setBusy(true)
            try {
                syncJob = AppServices.api.startSync()
                renderSyncJob()
                pollSync(syncJob!!.id)
            } catch (exception: Exception) {
                handleError(exception)
            } finally {
                setBusy(false)
            }
        }
    }

    private fun cancelSync() {
        val job = syncJob ?: return
        lifecycleScope.launch {
            try {
                syncJob = AppServices.api.cancelJob(job.id)
                renderSyncJob()
            } catch (exception: Exception) {
                handleError(exception)
            }
        }
    }

    private fun retrySync() {
        val job = syncJob ?: return
        lifecycleScope.launch {
            try {
                syncJob = AppServices.api.retryJob(job.id)
                renderSyncJob()
                pollSync(job.id)
            } catch (exception: Exception) {
                handleError(exception)
            }
        }
    }

    private fun pollSync(jobId: String) {
        syncPoller?.cancel()
        syncPoller = lifecycleScope.launch {
            while (true) {
                delay(1_500)
                try {
                    val latest = AppServices.api.getJob(jobId)
                    syncJob = latest
                    renderSyncJob()
                    if (!latest.isActive) {
                        showMessage(if (latest.status == "Completed") "视频同步完成。" else "同步任务已结束。")
                        loadAll()
                        break
                    }
                } catch (exception: Exception) {
                    handleError(exception)
                    break
                }
            }
        }
    }

    private fun renderSyncJob() {
        val job = syncJob
        binding.syncProgress.progress = job?.progress ?: 0
        binding.syncStatus.text = if (job == null) {
            "尚无同步任务"
        } else {
            buildString {
                append("${statusLabel(job.status)} · ${job.stage} · ${job.progress}%")
                job.currentItem?.takeIf { it.isNotBlank() }?.let { append("\n$it") }
                job.error?.takeIf { it.isNotBlank() }?.let { append("\n$it") }
            }
        }
        binding.startSyncButton.isEnabled = job?.isActive != true
        binding.cancelSyncButton.visibility = if (job?.isActive == true) View.VISIBLE else View.GONE
        binding.retrySyncButton.visibility = if (job?.canRetry == true) View.VISIBLE else View.GONE
    }

    private fun createBackup() {
        lifecycleScope.launch {
            setBusy(true)
            try {
                AppServices.api.createBackup()
                showMessage("数据库备份已创建。")
                loadAll()
            } catch (exception: Exception) {
                handleError(exception)
            } finally {
                setBusy(false)
            }
        }
    }

    private fun performAndReload(message: String, action: suspend () -> Unit) {
        lifecycleScope.launch {
            setBusy(true)
            try {
                action()
                showMessage(message)
                loadAll()
            } catch (exception: Exception) {
                handleError(exception)
            } finally {
                setBusy(false)
            }
        }
    }

    private fun showNameDialog(title: String, initial: String = "", action: (String) -> Unit) {
        val input = EditText(this).apply {
            setText(initial)
            setSelection(text.length)
            setSingleLine()
            setTextColor(getColor(R.color.dc_text))
            hint = "名称"
        }
        val wrapper = LinearLayout(this).apply {
            setPadding(24.dp, 4.dp, 24.dp, 0)
            addView(input, ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT)
        }
        val dialog = MaterialAlertDialogBuilder(this)
            .setTitle(title)
            .setView(wrapper)
            .setNegativeButton("取消", null)
            .setPositiveButton("保存", null)
            .create()
        dialog.setOnShowListener {
            dialog.getButton(android.app.AlertDialog.BUTTON_POSITIVE).setOnClickListener {
                val name = input.text.toString().trim()
                if (name.isBlank()) {
                    input.error = "名称不能为空"
                } else {
                    dialog.dismiss()
                    action(name)
                }
            }
        }
        dialog.show()
    }

    private fun confirm(title: String, message: String, action: () -> Unit) {
        MaterialAlertDialogBuilder(this)
            .setTitle(title)
            .setMessage(message)
            .setNegativeButton("取消", null)
            .setPositiveButton("确定") { _, _ -> action() }
            .show()
    }

    private fun setBusy(busy: Boolean) {
        binding.pageProgress.visibility = if (busy) View.VISIBLE else View.GONE
        binding.createBackupButton.isEnabled = !busy
        binding.importBackupButton.isEnabled = !busy
        binding.addCategoryButton.isEnabled = !busy
    }

    private fun handleError(exception: Exception) {
        if (exception is UnauthorizedException) {
            AppServices.session.clearAuthentication()
            startActivity(
                Intent(this, LoginActivity::class.java)
                    .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK)
            )
            finish()
            return
        }
        showMessage((exception as? ApiException)?.message ?: exception.message ?: "操作失败。")
    }

    private fun showMessage(message: String) {
        Snackbar.make(binding.root, message, Snackbar.LENGTH_LONG).show()
    }

    private fun card(content: View): MaterialCardView = MaterialCardView(this).apply {
        setCardBackgroundColor(getColor(R.color.dc_surface))
        radius = 8.dp.toFloat()
        strokeColor = getColor(R.color.dc_border)
        strokeWidth = 1.dp
        addView(content)
        layoutParams = LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT,
            ViewGroup.LayoutParams.WRAP_CONTENT
        ).apply { bottomMargin = 8.dp }
    }

    private fun verticalContainer(padding: Int) = LinearLayout(this).apply {
        orientation = LinearLayout.VERTICAL
        setPadding(padding.dp, padding.dp, padding.dp, padding.dp)
    }

    private fun horizontalActions() = LinearLayout(this).apply {
        gravity = Gravity.END
        orientation = LinearLayout.HORIZONTAL
        setPadding(0, 8.dp, 0, 0)
    }

    private fun actionButton(label: String, danger: Boolean = false, action: () -> Unit) =
        MaterialButton(this, null, com.google.android.material.R.attr.materialButtonOutlinedStyle).apply {
            text = label
            isAllCaps = false
            if (danger) setTextColor(getColor(R.color.dc_error))
            setOnClickListener { action() }
        }

    private fun titleText(value: String) = TextView(this).apply {
        text = value
        textSize = 16f
        setTextColor(getColor(R.color.dc_text))
        setTypeface(typeface, Typeface.BOLD)
    }

    private fun bodyText(value: String) = TextView(this).apply {
        text = value
        textSize = 13f
        setTextColor(getColor(R.color.dc_text_muted))
        setPadding(0, 5.dp, 0, 0)
    }

    private fun errorText(value: String) = bodyText(value).apply {
        setTextColor(getColor(R.color.dc_error))
    }

    private fun emptyText(value: String) = bodyText(value).apply {
        setPadding(4.dp, 10.dp, 4.dp, 14.dp)
    }

    private fun weighted() = LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f)

    private fun statusLabel(status: String) = when (status) {
        "Queued" -> "等待中"
        "Running" -> "执行中"
        "Completed" -> "已完成"
        "Failed" -> "失败"
        "Cancelled" -> "已取消"
        "Trashed" -> "回收站"
        "Missing" -> "文件缺失"
        else -> status
    }

    private val Int.dp: Int
        get() = (this * resources.displayMetrics.density).toInt()

    private data class LoadedData(
        val categories: List<TagCategory>,
        val maintenance: List<MaintenanceVideo>,
        val backups: List<BackupPackage>,
        val jobs: List<MediaJob>
    )
}
