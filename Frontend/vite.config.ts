// import { sveltekit } from '@sveltejs/kit/vite';
// import { defineConfig } from 'vite';

// export default defineConfig({
//   plugins: [sveltekit()],
//   server: {
//     https: true,
//     port: 5173,
//     proxy: {
//       '/bff': { target: 'https://localhost:7235', changeOrigin: false, secure: false },
//       '/connect': { target: 'https://localhost:7235', changeOrigin: false, secure: false }
//     }
//   }
// });

import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';
import mkcert from 'vite-plugin-mkcert';

export default defineConfig({
  plugins: [sveltekit(), mkcert()],
  server: {
    https: true,
    host: 'localhost',
    port: 5173,
    hmr: { protocol: 'wss', host: 'localhost', port: 5173 }
  }
});