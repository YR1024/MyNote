package com.dreamycinema.android.util

import org.junit.Assert.assertEquals
import org.junit.Assert.assertThrows
import org.junit.Test

class ServerAddressTest {
    @Test
    fun normalizesLanAddress() {
        assertEquals(
            "http://192.168.28.50:5210",
            ServerAddress.normalize(" 192.168.28.50:5210/ ")
        )
    }

    @Test
    fun resolvesApiPath() {
        assertEquals(
            "http://192.168.1.5:5210/api/videos",
            ServerAddress.resolve("http://192.168.1.5:5210", "/api/videos")
        )
    }

    @Test
    fun rejectsCredentialsInAddress() {
        assertThrows(IllegalArgumentException::class.java) {
            ServerAddress.normalize("http://admin:secret@192.168.1.5:5210")
        }
    }
}
