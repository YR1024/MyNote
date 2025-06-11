<template>
  <div class="container">
    <div v-for="(video, index) in videoList" :key="index" class="video-item">
      <!-- 复选框 -->
      <input
        type="checkbox"
        v-model="selectedVideos"
        :value="index"
        :disabled="
          selectedVideos.length >= 2 && !selectedVideos.includes(index)
        "
      />

      <!-- 重命名模块 -->
      <div v-if="renamingIndex === index">
        <input v-model="newName" />
        <button @click="confirmRename(index)">确认</button>
      </div>
      <span v-else>{{ video.Name }}</span>

      <!-- 播放器 -->
      <video
        v-if="currentVideo === index"
        :src="videoPath(video)"
        controls
        width="320"
        height="240"
      ></video>

      <!-- 操作按钮 -->
      <div class="actions">
        <button @click="togglePlay(index)">
          {{ currentVideo === index ? "停止" : "播放" }}
        </button>
        <button @click="deleteVideo(index)">删除</button>
        <button @click="startRename(index)">重命名</button>
      </div>
    </div>

    <button @click="mergeVideos" :disabled="selectedVideos.length !== 2">
      合并选中视频
    </button>
  </div>
</template>

<script>
export default {
  name: "HomeView",
  components: {},
  data() {
    return {
      videoList: [],
      selectedVideos: [], // 已选中的合并视频
      renamingIndex: null, // 正在重命名的索引
      newName: "", // 新文件名
      currentVideo: null, // 当前播放的视频

      blobUrls: new Map(), // 存储生成的Blob URL
    };
  },
  mounted() {
    this.init();
  },
  methods: {
    async init() {
      try {
        const res = await this.$api.getAllVideoFiles({
          path: "F:\\VideoWeb\\publish\\wwwroot\\Video\\AV",
        });
        // console.log(res);
        this.videoList = res.data;
      } catch (e) {
        console.error("API请求失败:", e);
      }
    },
    async deleteVideo(index) {
      try {
        await this.$api.deleteVideo(this.videoList[index].path);
        this.videoList.splice(index, 1);
      } catch (error) {
        console.error("删除失败", error);
      }
    },
    togglePlay(index) {
      this.currentVideo = this.currentVideo === index ? null : index;
    },
    videoPath(video) {
      console.log(video);
      // return '../../Video/AV/' + video.ReleatviePath;
      // return  video.FullPath;
      // return await this.$api.GetVideoStream({
      //     path: video.FullPath,
      //   });
      // return "/api/video/GetVideoStream?path=F:\\VideoWeb\\publish\\wwwroot\\Video\\AV\\jul-953-C\\jul-953-C.mp4";
      return `./api/api/video/video?_videoPath=${video.FullPath}`;
      // return "http://localhost:5142/api/values/video";

      // try {
      //   const response = await this.$api.GetVideoStream({
      //     path: video.FullPath,
      //   });

      //   // 创建Blob URL
      //   const blob = new Blob([response], { type: response.type });
      //   return URL.createObjectURL(blob);
      // } catch (error) {
      //   console.error("获取视频流失败:", error);
      //   return ""; // 返回空路径或占位符
      // }
    },
    handleSeeking() {
      if (this.currentSource) {
        this.currentSource.cancel("Seeking operation");
        URL.revokeObjectURL(this.currentUrl); // 释放Blob资源
      }
      this.currentSource = this.$axios.CancelToken.source();
    },
    startRename(index) {
      this.renamingIndex = index;
      this.newName = this.videoList[index].name;
    },
    async confirmRename(index) {
      try {
        await this.$api.renameVideo({
          path: this.videoList[index].path,
          newName: this.newName,
        });
        this.videoList[index].name = this.newName;
        this.renamingIndex = null;
      } catch (error) {
        console.error("重命名失败", error);
      }
    },
    async mergeVideos() {
      if (this.selectedVideos.length === 2) {
        try {
          const res = await this.$api.mergeVideos({
            file1: this.videoList[this.selectedVideos[0]].path,
            file2: this.videoList[this.selectedVideos[1]].path,
          });
          alert(res.message);
          this.selectedVideos = [];
        } catch (error) {
          console.error("合并失败", error);
        }
      }
    },
  },
  computed: {},
  watch: {},
};
</script>
<style scoped>
.video-item {
  margin: 20px auto;
  padding: 15px;
  border: 1px solid #eee;
  max-width: 600px;
}

.actions {
  margin-top: 10px;
}

.actions button {
  margin-right: 15px;
}
</style>
