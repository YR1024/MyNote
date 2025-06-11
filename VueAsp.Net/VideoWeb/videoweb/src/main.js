// import { createApp } from 'vue'
// import App from './App.vue'
// import router from './router'
// import store from './store'
// import api from './core/index'; // 引用刚才封装好的core下的index
 
// createApp.prototype.$api = api; //全局挂在api
// createApp(App).use(store).use(router).mount('#app')

import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import store from './store'
import api from './core/index';
import ElementPlus from 'element-plus'

const baseURL = "http://localhost:5142/";
const app = createApp(App)
app.config.globalProperties.$api = api // 正确挂载方式const app = createApp(App)
app.config.globalProperties.$baseURL = baseURL // 添加全局基础地址

app.use(store)
   .use(router)
   .use(ElementPlus)
   .mount('#app')