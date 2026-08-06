package com.dreamycinema.android.ui

import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.lifecycle.LifecycleCoroutineScope
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.dreamycinema.android.R
import com.dreamycinema.android.data.ApiClient
import com.dreamycinema.android.databinding.ItemVideoBinding
import com.dreamycinema.android.model.TagItem
import com.dreamycinema.android.model.VideoItem
import com.dreamycinema.android.util.formatBytes
import com.dreamycinema.android.util.formatDate
import com.dreamycinema.android.util.formatDuration
import com.google.android.material.chip.Chip

class VideoAdapter(
    private val scope: LifecycleCoroutineScope,
    private val api: ApiClient,
    private val onVideoClick: (VideoItem) -> Unit,
    private val onTagClick: (TagItem) -> Unit
) : ListAdapter<VideoItem, VideoAdapter.VideoViewHolder>(Diff) {

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): VideoViewHolder {
        val binding = ItemVideoBinding.inflate(LayoutInflater.from(parent.context), parent, false)
        return VideoViewHolder(binding)
    }

    override fun onBindViewHolder(holder: VideoViewHolder, position: Int) {
        holder.bind(getItem(position))
    }

    override fun onViewRecycled(holder: VideoViewHolder) {
        holder.binding.cover.tag = null
        holder.binding.cover.setImageResource(R.drawable.ic_video_placeholder)
        super.onViewRecycled(holder)
    }

    inner class VideoViewHolder(val binding: ItemVideoBinding) :
        RecyclerView.ViewHolder(binding.root) {
        fun bind(video: VideoItem) {
            binding.number.text = "番号：${video.number}"
            binding.title.text = video.title.ifBlank { "未命名" }
            binding.description.text = video.description
            binding.description.visibility = if (video.description.isBlank()) View.GONE else View.VISIBLE

            binding.tags.removeAllViews()
            binding.tags.visibility = if (video.tags.isEmpty()) View.GONE else View.VISIBLE
            video.tags.forEach { tag ->
                binding.tags.addView(Chip(binding.root.context).apply {
                    text = "#${tag.name}"
                    isCheckable = false
                    isClickable = true
                    minHeight = 32.dp
                    setEnsureMinTouchTargetSize(false)
                    setOnClickListener { onTagClick(tag) }
                })
            }

            val resolution = if (video.width != null && video.height != null) {
                "${video.width}x${video.height}"
            } else {
                "分辨率未知"
            }
            binding.meta.text = listOf(
                formatDate(video.createdAt),
                formatBytes(video.size),
                formatDuration(video.durationSeconds),
                resolution
            ).joinToString("  ·  ")

            binding.sourceStatus.text = when {
                video.originalAvailable && video.hardSubtitle != null ->
                    "可播放：原始视频 / 硬字幕版"
                video.originalAvailable -> "可播放：原始视频"
                video.hardSubtitle != null -> "可播放：硬字幕版（原始视频已删除）"
                else -> "当前没有可播放文件"
            }

            AuthenticatedImageLoader.load(binding.cover, video.coverUrl, scope, api)
            binding.root.setOnClickListener { onVideoClick(video) }
        }
    }

    private object Diff : DiffUtil.ItemCallback<VideoItem>() {
        override fun areItemsTheSame(oldItem: VideoItem, newItem: VideoItem): Boolean =
            oldItem.id == newItem.id

        override fun areContentsTheSame(oldItem: VideoItem, newItem: VideoItem): Boolean =
            oldItem == newItem
    }

    private val Int.dp: Int
        get() = (this * android.content.res.Resources.getSystem().displayMetrics.density).toInt()
}
