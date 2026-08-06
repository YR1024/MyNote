package com.dreamycinema.android

import android.app.Application
import com.dreamycinema.android.data.AppServices

class DreamyCinemaApplication : Application() {
    override fun onCreate() {
        super.onCreate()
        AppServices.initialize(this)
    }
}
