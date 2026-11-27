// Dev server proxy: the app calls relative /api and /hubs paths, and this forwards them to the API.
// Under Aspire, the API's address arrives in an environment variable; otherwise use its launch profile port.
const target =
  process.env['API_HTTP'] ??
  process.env['services__api__http__0'] ??
  'http://localhost:5241';

export default {
  '/api': { target, secure: false, changeOrigin: true },
  '/hubs': { target, secure: false, changeOrigin: true, ws: true },
};
