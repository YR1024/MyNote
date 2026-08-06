package com.dreamycinema.android.util

import java.util.Locale
import kotlin.math.roundToInt

fun formatBytes(value: Long): String {
    if (value < 1024) return "$value B"
    val units = arrayOf("KB", "MB", "GB", "TB")
    var size = value.toDouble()
    var unit = -1
    while (size >= 1024 && unit < units.lastIndex) {
        size /= 1024
        unit++
    }
    return if (size >= 10 || unit < 1) {
        String.format(Locale.US, "%.1f %s", size, units[unit])
    } else {
        String.format(Locale.US, "%.2f %s", size, units[unit])
    }
}

fun formatDuration(seconds: Double?): String {
    if (seconds == null || seconds <= 0) return "--:--"
    val total = seconds.roundToInt()
    val hours = total / 3600
    val minutes = (total % 3600) / 60
    val remaining = total % 60
    return if (hours > 0) {
        String.format(Locale.US, "%d:%02d:%02d", hours, minutes, remaining)
    } else {
        String.format(Locale.US, "%02d:%02d", minutes, remaining)
    }
}

fun formatDate(value: String): String =
    value.take(10).replace("-", ".")
