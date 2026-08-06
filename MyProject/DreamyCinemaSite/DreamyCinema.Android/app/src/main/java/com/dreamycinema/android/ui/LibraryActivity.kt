package com.dreamycinema.android.ui

import android.content.Intent
import android.os.Bundle
import android.view.Menu
import android.view.View
import android.view.ViewGroup
import android.view.inputmethod.EditorInfo
import android.view.inputmethod.InputMethodManager
import android.widget.AdapterView
import android.widget.ArrayAdapter
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.getSystemService
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.dreamycinema.android.R
import com.dreamycinema.android.data.ApiException
import com.dreamycinema.android.data.AppServices
import com.dreamycinema.android.data.UnauthorizedException
import com.dreamycinema.android.databinding.ActivityLibraryBinding
import com.dreamycinema.android.databinding.DialogFiltersBinding
import com.dreamycinema.android.model.TagCategory
import com.dreamycinema.android.model.VideoItem
import com.dreamycinema.android.model.VideoSort
import com.dreamycinema.android.util.applySafeDrawingInsets
import com.google.android.material.bottomsheet.BottomSheetBehavior
import com.google.android.material.bottomsheet.BottomSheetDialog
import com.google.android.material.chip.Chip
import com.google.android.material.chip.ChipGroup
import com.google.android.material.snackbar.Snackbar
import kotlinx.coroutines.async
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.launch

class LibraryActivity : AppCompatActivity() {
    private lateinit var binding: ActivityLibraryBinding
    private lateinit var adapter: VideoAdapter

    private val videos = mutableListOf<VideoItem>()
    private var categories: List<TagCategory> = emptyList()
    private val selectedTagIds = linkedSetOf<Int>()
    private var selectedSort = VideoSort.RECENT
    private var currentPage = 0
    private var hasMore = false
    private var loading = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityLibraryBinding.inflate(layoutInflater)
        setContentView(binding.root)
        applySafeDrawingInsets(binding.root)

        setupToolbar()
        setupSearchAndSort()
        setupList()
        binding.filterButton.setOnClickListener { showFilterDialog() }
        loadInitial()
    }

    private fun setupToolbar() {
        binding.toolbar.menu.add(Menu.NONE, MENU_MANAGE, Menu.NONE, "管理")
            .setShowAsAction(android.view.MenuItem.SHOW_AS_ACTION_ALWAYS)
        binding.toolbar.menu.add(Menu.NONE, MENU_SYNC, Menu.NONE, "同步")
            .setShowAsAction(android.view.MenuItem.SHOW_AS_ACTION_IF_ROOM)
        binding.toolbar.menu.add(Menu.NONE, MENU_REFRESH, Menu.NONE, "刷新")
            .setShowAsAction(android.view.MenuItem.SHOW_AS_ACTION_NEVER)
        binding.toolbar.menu.add(Menu.NONE, MENU_LOGOUT, Menu.NONE, "退出")
            .setShowAsAction(android.view.MenuItem.SHOW_AS_ACTION_NEVER)
        binding.toolbar.setOnMenuItemClickListener {
            when (it.itemId) {
                MENU_MANAGE -> {
                    startActivity(Intent(this, ManagementActivity::class.java))
                    true
                }
                MENU_SYNC -> {
                    lifecycleScope.launch {
                        try {
                            AppServices.api.startSync()
                            Snackbar.make(
                                binding.root,
                                "同步任务已提交，可在管理中心查看进度。",
                                Snackbar.LENGTH_LONG
                            ).show()
                        } catch (exception: Exception) {
                            handleError(exception)
                        }
                    }
                    true
                }
                MENU_REFRESH -> {
                    loadInitial()
                    true
                }
                MENU_LOGOUT -> {
                    lifecycleScope.launch {
                        AppServices.api.logout()
                        openLogin()
                    }
                    true
                }
                else -> false
            }
        }
    }

    private fun setupSearchAndSort() {
        val sorts = VideoSort.entries
        binding.sortSpinner.adapter = ArrayAdapter(
            this,
            android.R.layout.simple_spinner_dropdown_item,
            sorts.map { it.displayName }
        )
        binding.sortSpinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
            override fun onItemSelected(parent: AdapterView<*>?, view: View?, position: Int, id: Long) {
                val next = sorts[position]
                if (next != selectedSort) {
                    selectedSort = next
                    reloadVideos()
                }
            }

            override fun onNothingSelected(parent: AdapterView<*>?) = Unit
        }

        binding.searchButton.setOnClickListener { submitSearch() }
        binding.searchInput.setOnEditorActionListener { _, actionId, _ ->
            if (actionId == EditorInfo.IME_ACTION_SEARCH) {
                submitSearch()
                true
            } else {
                false
            }
        }
    }

    private fun setupList() {
        val layoutManager = LinearLayoutManager(this)
        adapter = VideoAdapter(
            scope = lifecycleScope,
            api = AppServices.api,
            onVideoClick = { video ->
                startActivity(
                    Intent(this, PlayerActivity::class.java)
                        .putExtra(PlayerActivity.EXTRA_VIDEO_ID, video.id)
                )
            },
            onTagClick = { tag ->
                if (selectedTagIds.add(tag.id)) {
                    updateFilterSummary()
                    binding.videoList.scrollToPosition(0)
                    reloadVideos()
                }
            }
        )
        binding.videoList.layoutManager = layoutManager
        binding.videoList.adapter = adapter
        binding.videoList.addOnScrollListener(object : RecyclerView.OnScrollListener() {
            override fun onScrolled(recyclerView: RecyclerView, dx: Int, dy: Int) {
                if (dy <= 0 || loading || !hasMore) return
                val lastVisible = layoutManager.findLastVisibleItemPosition()
                if (lastVisible >= adapter.itemCount - 4) loadNextPage()
            }
        })
    }

    private fun loadInitial() {
        if (loading) return
        loading = true
        binding.topProgress.visibility = View.VISIBLE
        lifecycleScope.launch {
            try {
                val result = coroutineScope {
                    val categoryTask = async { AppServices.api.getTagCategories() }
                    val videosTask = async {
                        AppServices.api.getVideos(
                            search = binding.searchInput.text?.toString().orEmpty(),
                            sort = selectedSort,
                            tagIds = selectedTagIds,
                            page = 1
                        )
                    }
                    categoryTask.await() to videosTask.await()
                }
                categories = result.first
                videos.clear()
                videos.addAll(result.second.items)
                currentPage = result.second.page
                hasMore = result.second.hasMore
                adapter.submitList(videos.toList())
                updateHeader(result.second.total)
                updateFilterSummary()
                binding.emptyText.visibility = if (videos.isEmpty()) View.VISIBLE else View.GONE
            } catch (exception: Exception) {
                handleError(exception)
            } finally {
                loading = false
                binding.topProgress.visibility = View.GONE
            }
        }
    }

    private fun reloadVideos() {
        if (loading) return
        loading = true
        binding.topProgress.visibility = View.VISIBLE
        lifecycleScope.launch {
            try {
                val page = AppServices.api.getVideos(
                    search = binding.searchInput.text?.toString().orEmpty(),
                    sort = selectedSort,
                    tagIds = selectedTagIds,
                    page = 1
                )
                videos.clear()
                videos.addAll(page.items)
                currentPage = page.page
                hasMore = page.hasMore
                adapter.submitList(videos.toList())
                updateHeader(page.total)
                binding.emptyText.visibility = if (videos.isEmpty()) View.VISIBLE else View.GONE
            } catch (exception: Exception) {
                handleError(exception)
            } finally {
                loading = false
                binding.topProgress.visibility = View.GONE
            }
        }
    }

    private fun loadNextPage() {
        loading = true
        binding.pageProgress.visibility = View.VISIBLE
        lifecycleScope.launch {
            try {
                val page = AppServices.api.getVideos(
                    search = binding.searchInput.text?.toString().orEmpty(),
                    sort = selectedSort,
                    tagIds = selectedTagIds,
                    page = currentPage + 1
                )
                videos.addAll(page.items)
                currentPage = page.page
                hasMore = page.hasMore
                adapter.submitList(videos.toList())
                updateHeader(page.total)
            } catch (exception: Exception) {
                handleError(exception)
            } finally {
                loading = false
                binding.pageProgress.visibility = View.GONE
            }
        }
    }

    private fun submitSearch() {
        binding.searchInput.clearFocus()
        getSystemService<InputMethodManager>()?.hideSoftInputFromWindow(
            binding.searchInput.windowToken,
            0
        )
        reloadVideos()
    }

    private fun showFilterDialog() {
        val dialog = BottomSheetDialog(this)
        val dialogBinding = DialogFiltersBinding.inflate(layoutInflater)
        val draft = selectedTagIds.toMutableSet()
        val chips = mutableListOf<Chip>()

        categories.forEach { category ->
            dialogBinding.categoryContainer.addView(TextView(this).apply {
                text = category.name
                textSize = 15f
                setTextColor(getColor(R.color.dc_text_muted))
                setPadding(0, 16.dp, 0, 6.dp)
            })

            val group = ChipGroup(this).apply {
                isSingleSelection = false
                isSelectionRequired = false
                chipSpacingHorizontal = 6.dp
                chipSpacingVertical = 4.dp
            }
            category.tags.forEach { tag ->
                val chip = Chip(this).apply {
                    id = View.generateViewId()
                    text = "#${tag.name}  ${tag.videoCount}"
                    isCheckable = true
                    isChecked = tag.id in draft
                    setOnCheckedChangeListener { _, checked ->
                        if (checked) draft += tag.id else draft -= tag.id
                    }
                }
                chips += chip
                group.addView(chip)
            }
            dialogBinding.categoryContainer.addView(group)
        }

        if (categories.isEmpty()) {
            dialogBinding.categoryContainer.addView(TextView(this).apply {
                text = "暂无标签"
                setTextColor(getColor(R.color.dc_text_muted))
            })
        }

        dialogBinding.clearButton.setOnClickListener {
            draft.clear()
            chips.forEach { it.isChecked = false }
        }
        dialogBinding.applyButton.setOnClickListener {
            selectedTagIds.clear()
            selectedTagIds.addAll(draft)
            updateFilterSummary()
            dialog.dismiss()
            reloadVideos()
        }

        dialog.setContentView(dialogBinding.root)
        dialog.setOnShowListener {
            val sheet = dialog.findViewById<View>(
                com.google.android.material.R.id.design_bottom_sheet
            )
            sheet?.layoutParams?.height = ViewGroup.LayoutParams.MATCH_PARENT
            sheet?.let {
                BottomSheetBehavior.from(it).apply {
                    state = BottomSheetBehavior.STATE_EXPANDED
                    skipCollapsed = true
                }
            }
        }
        dialog.show()
    }

    private fun updateFilterSummary() {
        val names = categories
            .flatMap { it.tags }
            .filter { it.id in selectedTagIds }
            .map { "#${it.name}" }
        binding.filterSummary.text = if (names.isEmpty()) {
            "未选择标签"
        } else {
            "已按交集筛选：${names.joinToString("  ")}"
        }
        binding.filterButton.text = if (names.isEmpty()) "标签筛选" else "标签（${names.size}）"
    }

    private fun updateHeader(total: Int) {
        binding.toolbar.subtitle = "已加载 ${videos.size} / 共 $total 个视频"
    }

    private fun handleError(exception: Exception) {
        if (exception is UnauthorizedException) {
            AppServices.session.clearAuthentication()
            openLogin()
            return
        }
        val message = (exception as? ApiException)?.message
            ?: exception.message
            ?: "加载失败。"
        Snackbar.make(binding.root, message, Snackbar.LENGTH_LONG).show()
    }

    private fun openLogin() {
        startActivity(
            Intent(this, LoginActivity::class.java)
                .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK)
        )
        finish()
    }

    override fun onRestart() {
        super.onRestart()
        loadInitial()
    }

    private val Int.dp: Int
        get() = (this * resources.displayMetrics.density).toInt()

    companion object {
        private const val MENU_REFRESH = 1
        private const val MENU_LOGOUT = 2
        private const val MENU_MANAGE = 3
        private const val MENU_SYNC = 4
    }
}
