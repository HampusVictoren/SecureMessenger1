

// import { sveltekit } from '@sveltejs/kit/vite';
// import mkcert from 'vite-plugin-mkcert';
// import { defineConfig } from 'vite';

// export default defineConfig({
//   plugins: [sveltekit(), mkcert()],
//   server: {
//     https: true,
//     proxy: {
//       '/api': {
//         target: 'https://localhost:7235', // point to your backend
//         changeOrigin: false,
//         secure: false
//       },
//       '/hubs': {
//         target: 'https://localhost:7235',
//         ws: true,
//         changeOrigin: false,
//         secure: false
//       },
//       '/bff': {
//         target: 'https://localhost:7235',
//         changeOrigin: false,
//         secure: false
//       }
//     }
//   }
// });


// import { sveltekit } from '@sveltejs/kit/vite';
// import { defineConfig } from 'vite';
// import mkcert from 'vite-plugin-mkcert';

// export default defineConfig({
//   plugins: [sveltekit(), mkcert()],
//   server: {
//     https: true,
//     host: 'localhost',
//     port: 5173,
//     hmr: { protocol: 'wss', host: 'localhost', port: 5173 },
//     proxy: {
//       '/api': { target: 'https://localhost:7235', changeOrigin: false, secure: false },
//       '/bff': { target: 'https://localhost:7235', changeOrigin: false, secure: false },
//       '/connect': { target: 'https://localhost:7235', changeOrigin: false, secure: false },
//       '/Identity': { target: 'https://localhost:7235', changeOrigin: false, secure: false },
//       '/hubs': { target: 'https://localhost:7235', ws: true, changeOrigin: false, secure: false }
//     }
//   }
// });

import { sveltekit } from '@sveltejs/kit/vite';
import mkcert from 'vite-plugin-mkcert';
import { defineConfig } from 'vite';

export default defineConfig({
  plugins: [sveltekit(), mkcert()],
  server: {
    https: true,
    // IMPORTANT: remove proxies for /api, /hubs, /bff to avoid loops
    // proxy: { }
  }
});