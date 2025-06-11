
import { createRouter, createWebHistory } from 'vue-router';
//import home from '../views/home/index.vue';

const routes = [
  {
    path: '/',
    component: () => import('@/views/HomeView.vue')
  },
  {
    path: '/home',
    name: 'Home',
    component: () => import('@/views/HomeView.vue')
  },
  {
    path: '/star',
    name: 'Star',
    //component: () => import('@/views/star')
    component: () => import('@/views/StarView.vue')
  },
];

const router = createRouter({
  history: createWebHistory("./"),
  routes
});

export default router;