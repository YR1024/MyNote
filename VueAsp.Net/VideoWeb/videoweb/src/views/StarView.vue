<template>
  <div class="about">
    <h1>This is an about page</h1>
  </div>
</template>

<script>
export default {
  name: "StarView",
  data() {
    return {
      msg: "Welcome to Your Vue.js App",
    };
  },
  watch: {},
  mounted() {
    console.log("StarView mounted");
    this.init();
  },
  methods: {
    async init() {
      console.log(this.$api);
      try {
        // this.$api.test().then((res) => {
        const res = await this.$api.test();
        console.log(res);
        // });
        // 处理正常数据
      } catch (e) {
        console.error("API请求失败:", e);
        // 可选：特定错误处理逻辑
        // if(e.response?.status === 500) { ... }
      }
      // catch(e){
      //   console.error("Error in init:", e);
      // }
    },
    handleClick() {
      console.log("Button clicked");
    },
  },
};
</script>
