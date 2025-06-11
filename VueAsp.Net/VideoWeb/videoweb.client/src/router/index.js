// src/router/index.js
import { createRouter, createWebHistory } from 'vue-router';
//import home from '../views/home/index.vue';

const routes = [
  {
    path: '/',
    component: () => import('@/views/home/indexPage.vue')
  },
  {
    path: '/home',
    name: 'Home',
    component: () => import('@/views/home/indexPage.vue')
  },
  {
    path: '/star',
    name: 'Star',
    //component: () => import('@/views/star')
    component: () => import('@/views/star/index.vue')
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

export default router;
