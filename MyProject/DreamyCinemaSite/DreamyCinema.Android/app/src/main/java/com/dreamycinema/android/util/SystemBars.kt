package com.dreamycinema.android.util

import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.ViewCompat
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.updatePadding

fun AppCompatActivity.applySafeDrawingInsets(root: View) {
    WindowCompat.setDecorFitsSystemWindows(window, false)
    val initialLeft = root.paddingLeft
    val initialTop = root.paddingTop
    val initialRight = root.paddingRight
    val initialBottom = root.paddingBottom

    ViewCompat.setOnApplyWindowInsetsListener(root) { view, windowInsets ->
        val safeInsets = windowInsets.getInsets(
            WindowInsetsCompat.Type.systemBars() or WindowInsetsCompat.Type.displayCutout()
        )
        view.updatePadding(
            left = initialLeft + safeInsets.left,
            top = initialTop + safeInsets.top,
            right = initialRight + safeInsets.right,
            bottom = initialBottom + safeInsets.bottom
        )
        windowInsets
    }
    ViewCompat.requestApplyInsets(root)
}
