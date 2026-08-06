package com.dreamycinema.android.ui

import android.graphics.Bitmap
import android.graphics.BitmapFactory
import android.util.LruCache
import android.widget.ImageView
import com.dreamycinema.android.R
import com.dreamycinema.android.data.ApiClient
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch

object AuthenticatedImageLoader {
    private val cache = object : LruCache<String, Bitmap>(24 * 1024 * 1024) {
        override fun sizeOf(key: String, value: Bitmap): Int = value.byteCount
    }

    fun load(
        imageView: ImageView,
        path: String?,
        scope: CoroutineScope,
        api: ApiClient
    ) {
        imageView.tag = path
        if (path == null) {
            imageView.setImageResource(R.drawable.ic_video_placeholder)
            return
        }

        cache.get(path)?.let {
            imageView.setImageBitmap(it)
            return
        }

        imageView.setImageResource(R.drawable.ic_video_placeholder)
        scope.launch {
            val bitmap = runCatching {
                val bytes = api.getBytes(path)
                BitmapFactory.decodeByteArray(bytes, 0, bytes.size)
            }.getOrNull() ?: return@launch

            cache.put(path, bitmap)
            if (imageView.tag == path) {
                imageView.setImageBitmap(bitmap)
            }
        }
    }
}
