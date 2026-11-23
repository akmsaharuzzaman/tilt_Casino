mergeInto(LibraryManager.library, {

    $ExternalGameLoader: {
        iframe: null,

        create: function (url) {

            if (ExternalGameLoader.iframe) {
                ExternalGameLoader.iframe.remove();
            }

            var iframe = document.createElement("iframe");

            iframe.id = "unity-external-game-frame";

            iframe.src = new URL(url, window.location.href).href;

            iframe.style.position = "fixed";
            iframe.style.border = "0";
            iframe.style.margin = "0";
            iframe.style.padding = "0";
            iframe.style.zIndex = "99999";
            iframe.style.background = "#000";
            iframe.style.display = "none";
            iframe.style.overflow = "hidden";

            iframe.setAttribute(
                "allow",
                "fullscreen; autoplay; gamepad; pointer-lock"
            );

            iframe.setAttribute("allowfullscreen", "true");

            iframe.setAttribute(
                "sandbox",
                "allow-scripts allow-same-origin allow-pointer-lock allow-forms"
            );

            document.body.appendChild(iframe);

            ExternalGameLoader.iframe = iframe;
        },

        setRect: function (x, y, width, height) {

            var iframe = ExternalGameLoader.iframe;

            if (!iframe || !Module.canvas)
                return;

            var canvasRect = Module.canvas.getBoundingClientRect();

            iframe.style.left =
                (canvasRect.left + x * canvasRect.width) + "px";

            iframe.style.top =
                (canvasRect.top + y * canvasRect.height) + "px";

            iframe.style.width =
                (width * canvasRect.width) + "px";

            iframe.style.height =
                (height * canvasRect.height) + "px";
        },

        setVisible: function (visible) {

            if (!ExternalGameLoader.iframe)
                return;

            ExternalGameLoader.iframe.style.display =
                visible ? "block" : "none";
        },

        destroy: function () {

            if (ExternalGameLoader.iframe) {

                ExternalGameLoader.iframe.remove();

                ExternalGameLoader.iframe = null;
            }
        }
    },

    ExternalGame_Create: function (urlPtr) {

        var url = UTF8ToString(urlPtr);

        ExternalGameLoader.create(url);
    },

    ExternalGame_SetRect: function (x, y, width, height) {

        ExternalGameLoader.setRect(x, y, width, height);
    },

    ExternalGame_SetVisible: function (visible) {

        ExternalGameLoader.setVisible(visible);
    },

    ExternalGame_Destroy: function () {

        ExternalGameLoader.destroy();
    }
});