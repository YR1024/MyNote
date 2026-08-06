package com.dreamycinema.android.util

import java.net.URI

object ServerAddress {
    fun normalize(value: String): String {
        var candidate = value.trim()
        require(candidate.isNotEmpty()) { "请输入服务器地址。" }
        if (!candidate.contains("://")) {
            candidate = "http://$candidate"
        }

        val uri = runCatching { URI(candidate) }
            .getOrElse { throw IllegalArgumentException("服务器地址格式不正确。") }
        require(uri.scheme.equals("http", true) || uri.scheme.equals("https", true)) {
            "服务器地址只支持 HTTP 或 HTTPS。"
        }
        require(!uri.host.isNullOrBlank()) { "服务器地址缺少主机名或 IP。" }
        require(uri.userInfo == null) { "服务器地址不能包含用户名或密码。" }

        val scheme = uri.scheme.lowercase()
        val authority = uri.rawAuthority
        return "$scheme://$authority"
    }

    fun resolve(baseUrl: String, path: String): String {
        if (path.startsWith("http://") || path.startsWith("https://")) {
            return path
        }
        return baseUrl.trimEnd('/') + "/" + path.trimStart('/')
    }
}
