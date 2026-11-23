# Unity 6 Responsive WebGL Template

A full-viewport responsive WebGL page with a minimal website-style top bar, fullscreen control, progress screen, and startup error handling.

## Install

1. Extract the ZIP.
2. Copy the `ResponsiveStudio` folder into `Assets/WebGLTemplates/`.
3. In Unity, open **Edit > Project Settings > Player > Resolution and Presentation**.
4. Select **ResponsiveStudio** as the WebGL Template.
5. Build for Web.

## Build layout

The template expects Unity's WebGL output build directory to be named `Build`. If you use a different name, change `buildUrl` in `template.js`.

Your post-build image-copy script can place the `Image` folder beside `index.html`:
- `index.html`
- `style.css`
- `template.js`
- `Build/`
- `Image/`

Then relative image paths such as `Image/logo.png` resolve from the page root.

## Performance and compatibility notes

- Unity controls the WebGL drawing buffer. The template uses `matchWebGLToCanvasSize` and caps the configured device pixel ratio at 2 to avoid unnecessarily large render buffers on high-DPI devices.
- CSS makes the canvas fill the available browser viewport and accounts for mobile safe areas.
- The Fullscreen button requests fullscreen after a user gesture. Some mobile browsers, especially iOS Safari, restrict the Fullscreen API; the page still fills the browser viewport.
- A template cannot make a large Unity `.data` / `.wasm` payload download faster by itself. For faster loading, use a Release build, Brotli or Gzip compression configured correctly on the server, reduce initial scene assets, and move optional content to Addressables/remote loading.
- Serve the site over HTTPS. For compressed builds, configure correct `Content-Encoding` and MIME types on the web server.
- Test on actual target devices. WebGL 2, GPU capability, memory limits, and fullscreen behavior vary by browser and device.
