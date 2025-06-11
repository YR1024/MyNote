import {
  get,
//   post,
//   postForm,
//   postJson,
  getBlob,
//   postBlob
} from './axios';
 
export default {
  // 示范
  test: args => get(`/WeatherForecast`, args),

  getAllVideoFiles: args => get(`/api/video/getAllVideoFiles`, args),


  GetVideoStream: args => getBlob(`/api/video/GetVideoStream`, 
    args),
 
};