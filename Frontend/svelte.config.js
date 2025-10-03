import adapter from '@sveltejs/adapter-static';

const dev = process.argv.includes('dev');

export default {
  kit: {
    adapter: adapter({
      pages: 'build',
      assets: 'build',
      fallback: 'index.html'
    }),
    paths: { base: '' },
   // trailingSlash: 'never'
  }
};