(function () {
  "use strict";

  const buildUrl = "Build";
  const loaderUrl = buildUrl + "/{{{ LOADER_FILENAME }}}";

  const config = {
    dataUrl: buildUrl + "/{{{ DATA_FILENAME }}}",
    frameworkUrl: buildUrl + "/{{{ FRAMEWORK_FILENAME }}}",
    codeUrl: buildUrl + "/{{{ CODE_FILENAME }}}",
#if MEMORY_FILENAME
    memoryUrl: buildUrl + "/{{{ MEMORY_FILENAME }}}",
#endif
#if SYMBOLS_FILENAME
    symbolsUrl: buildUrl + "/{{{ SYMBOLS_FILENAME }}}",
#endif
    streamingAssetsUrl: "StreamingAssets",
    companyName: "{{{ COMPANY_NAME }}}",
    productName: "{{{ PRODUCT_NAME }}}",
    productVersion: "{{{ PRODUCT_VERSION }}}",
    matchWebGLToCanvasSize: true,
    devicePixelRatio: Math.min(window.devicePixelRatio || 1, 2),
    showBanner: function (message, type) {
      const box = document.getElementById("error-message");
      if (!box) return;
      box.hidden = false;
      box.textContent = message;
      box.dataset.type = type || "error";
    }
  };

  const canvas = document.getElementById("unity-canvas");
  const loadingScreen = document.getElementById("loading-screen");
  const progressFill = document.getElementById("progress-fill");
  const progressValue = document.getElementById("progress-value");
  const loadingStatus = document.getElementById("loading-status");
  const fullscreenButton = document.getElementById("fullscreen-button");
  const gameShell = document.getElementById("game-shell");

  function setProgress(progress) {
    const percent = Math.max(0, Math.min(100, Math.round(progress * 100)));
    progressFill.style.width = percent + "%";
    progressValue.textContent = percent + "%";
    loadingStatus.textContent = percent >= 100
      ? "Starting experience…"
      : "Loading experience…";
  }

  function showError(message) {
    const box = document.getElementById("error-message");
    box.hidden = false;
    box.textContent = message;
  }

  fullscreenButton.addEventListener("click", function () {
    const request = gameShell.requestFullscreen ||
                    gameShell.webkitRequestFullscreen;

    if (document.fullscreenElement || document.webkitFullscreenElement) {
      const exit = document.exitFullscreen || document.webkitExitFullscreen;
      if (exit) exit.call(document);
      return;
    }

    if (!request) {
      showError("Fullscreen is not supported by this browser. The game will remain fitted to the browser window.");
      return;
    }

    try {
      const result = request.call(gameShell);
      if (result && typeof result.catch === "function") {
        result.catch(function () {
          showError("Fullscreen was blocked by the browser. The game will remain fitted to the browser window.");
        });
      }
    } catch (error) {
      showError("Fullscreen was blocked by the browser. The game will remain fitted to the browser window.");
    }
  });

  const script = document.createElement("script");
  script.src = loaderUrl;

  script.onload = function () {
    createUnityInstance(canvas, config, setProgress)
      .then(function (unityInstance) {
        window.unityInstance = unityInstance;
        setProgress(1);
        window.setTimeout(function () {
          loadingScreen.classList.add("is-hidden");
        }, 180);
      })
      .catch(function (message) {
        showError(message || "Unity could not start. Please refresh the page.");
        loadingStatus.textContent = "Unable to start";
      });
  };

  script.onerror = function () {
    showError("Unity loader could not be found. Check that the Build folder and loader file were uploaded correctly.");
  };

  document.body.appendChild(script);

  document.addEventListener("touchmove", function (event) {
    if (event.target === canvas) event.preventDefault();
  }, { passive: false });
})();
