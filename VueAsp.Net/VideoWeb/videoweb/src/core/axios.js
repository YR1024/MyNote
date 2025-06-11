// import Vue from 'vue';
import axios from "axios";
import qs from "qs";
// import baseUrl from '../config' // 配置地址
import router from "@/router";
// import {
//   Message
// } from 'element-ui'
//  import ElementPlus from 'element-plus'
import { ElMessage } from "element-plus";

// 配置接口地址
// const baseURL = baseUrl;
// const baseURL = "http://localhost:5142/";

axios.defaults.baseURL = "/api"; //开发环境
// axios.defaults.baseURL = "http://127.0.0.1:5142" //开发环境
// Vue.prototype.$baseURL = baseURL.dev.baseURL; //全局挂载地址

// 配置超时时间
axios.defaults.timeout = 30e3;

// 请求拦截器
axios.interceptors.request.use(
  (config) => {
    if (sessionStorage.getItem("token")) {
      // 判断是否存在token，如果存在的话，则每个http header都加上token
      config.headers["Authorization"] = sessionStorage.getItem("token");
    }
    return config;
  },
  (err) => Promise.reject(err)
);

// 响应拦截器
axios.interceptors.response.use(
  (response) => {
    return Promise.resolve(response);
  },
  (error) => {
    if (error) {
      const { response } = error;
      const errorMessage = response?.data?.message || '请求异常，请稍后重试'
      
      if (response.status === 417) {
        ElMessage.error("登录信息已过期，请重新登录");
        sessionStorage.clear();
        router.push(`/login`);
      } else {
+       ElMessage.error(`[${response.status}] ${errorMessage}`)
      }
    }
    return Promise.reject(error);
  }
);

export const postForms = (url, form) => {
  const ax = axios.post(url, form, {
    "Content-Type": "multipart/form-data",
  });
  return ax;
};

export const get = (url, arg = {}) => axios.get(`${url}?${qs.stringify(arg)}`);

export const post = (url, arg = {}) => axios.post(url, qs.stringify(arg));

export const postForm = (url, arg = {}, cfg = {}) => {
  const formData = new FormData();
  Object.entries(arg).forEach(([key, value]) => formData.append(key, value));
  return axios.post(url, formData, {
    "Content-Type": "multipart/form-data",
    ...cfg,
  });
};

export const put = (url, arg = {}) => axios.put(url, qs.stringify(arg));
export const putJson = (url, arg = {}) =>
  axios.put(url, arg, {
    headers: {
      "Content-type": "application/json;charset=UTF-8",
    },
  });

export const del = (url, arg = {}) =>
  axios.delete(url, {
    data: arg,
  });

export const postJson = (url, arg = {}) =>
  axios.post(url, arg, {
    headers: {
      "Content-type": "application/json;charset=UTF-8",
    },
  });

export const getBlob = (url, arg) =>
  axios.get(`${url}?${qs.stringify(arg)}`, {
    responseType: "blob",
  });

export const postBlob = (url, arg) =>
  axios.post(url, qs.stringify(arg), {
    responseType: "blob",
  });

export const postJsonBlob = (url, arg) =>
  axios.post(url, arg, {
    headers: {
      "Content-type": "application/json;charset=UTF-8",
    },
    responseType: "blob",
  });
