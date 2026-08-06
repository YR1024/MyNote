package com.dreamycinema.android.ui

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.view.inputmethod.EditorInfo
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.dreamycinema.android.data.ApiException
import com.dreamycinema.android.data.AppServices
import com.dreamycinema.android.databinding.ActivityLoginBinding
import com.dreamycinema.android.util.applySafeDrawingInsets
import kotlinx.coroutines.launch

class LoginActivity : AppCompatActivity() {
    private lateinit var binding: ActivityLoginBinding
    private var restoreAttempted = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityLoginBinding.inflate(layoutInflater)
        setContentView(binding.root)
        applySafeDrawingInsets(binding.root)

        binding.serverInput.setText(AppServices.session.serverUrl)
        binding.loginButton.setOnClickListener { login() }
        binding.passwordInput.setOnEditorActionListener { _, actionId, _ ->
            if (actionId == EditorInfo.IME_ACTION_DONE) {
                login()
                true
            } else {
                false
            }
        }

        if (AppServices.session.cookieHeader().isNotBlank()) {
            restoreSession()
        }
    }

    private fun restoreSession() {
        if (restoreAttempted) return
        restoreAttempted = true
        setBusy(true)
        lifecycleScope.launch {
            try {
                val session = AppServices.api.refreshSession()
                if (session.authenticated) {
                    openLibrary()
                } else {
                    AppServices.session.clearAuthentication()
                }
            } catch (_: Exception) {
                // Keep the login form available when the saved server is currently offline.
            } finally {
                if (!isFinishing) setBusy(false)
            }
        }
    }

    private fun login() {
        binding.serverLayout.error = null
        binding.passwordLayout.error = null
        showStatus(null)

        val server = binding.serverInput.text?.toString().orEmpty()
        val password = binding.passwordInput.text?.toString().orEmpty()
        try {
            AppServices.session.configureServer(server)
        } catch (exception: IllegalArgumentException) {
            binding.serverLayout.error = exception.message
            return
        }
        if (password.isBlank()) {
            binding.passwordLayout.error = "请输入管理员密码。"
            return
        }

        setBusy(true)
        lifecycleScope.launch {
            try {
                val session = AppServices.api.login(password)
                when {
                    session.authenticated -> openLibrary()
                    session.setupRequired -> showStatus("请先在服务器电脑浏览器中设置管理员密码。")
                    else -> showStatus("登录失败。")
                }
            } catch (exception: ApiException) {
                showStatus(exception.message)
            } catch (exception: Exception) {
                showStatus(exception.message ?: "登录失败。")
            } finally {
                if (!isFinishing) setBusy(false)
            }
        }
    }

    private fun setBusy(busy: Boolean) {
        binding.progress.visibility = if (busy) View.VISIBLE else View.GONE
        binding.loginButton.isEnabled = !busy
        binding.serverInput.isEnabled = !busy
        binding.passwordInput.isEnabled = !busy
    }

    private fun showStatus(message: String?) {
        binding.statusText.text = message.orEmpty()
        binding.statusText.visibility = if (message.isNullOrBlank()) View.GONE else View.VISIBLE
    }

    private fun openLibrary() {
        startActivity(Intent(this, LibraryActivity::class.java))
        finish()
    }
}
