mergeInto(LibraryManager.library, {
    // Reads the parent/host page URL
    GetParentURL: function () {
        var returnStr = window.location.href;
        try {
            // First attempt: Try standard same-origin access
            if (window.parent && window.parent.location && window.parent.location.href) {
                returnStr = window.parent.location.href;
            } else if (document.referrer && document.referrer !== "") {
                // Fallback 1: Get the parent domain/URL via referrer
                returnStr = document.referrer;
            } else if (window.location.ancestorOrigins && window.location.ancestorOrigins.length > 0) {
                // Fallback 2: Chromium specific domain tracking
                returnStr = window.location.ancestorOrigins[window.location.ancestorOrigins.length - 1];
            }
        } catch (e) {
            // Cross-origin exception hit, executing secure fallback routine
            if (document.referrer && document.referrer !== "") {
                returnStr = document.referrer;
            } else if (window.location.ancestorOrigins && window.location.ancestorOrigins.length > 0) {
                returnStr = window.location.ancestorOrigins[window.location.ancestorOrigins.length - 1];
            } else {
                console.warn("Cross-origin frame restriction completely blocked parent tracking. Using frame location.");
            }
        }

        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },

    // Reads the specific iframe/game window URL
    GetSelfURL: function () {
        var returnStr = window.location.href;
        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    },

    RedirectParentWindow: function (urlPtr) {
        // Convert the Unity string pointer to a JavaScript string
        var url = UTF8ToString(urlPtr);
        
        try {
            // Target the topmost browser window frame
            window.top.location.href = url;
        } catch (e) {
            // Fallback if strict iframe sandboxing blocks window.top access
            window.open(url, '_blank');
        }
    },

    JS_FileSystem_Sync: function () {
        FS.syncfs(false, function (err) {
            if (err) {
                console.error("IndexedDB sync error: ", err);
            }
        });
    },

    JS_UpdateVoiceProgress: function (current, total) {
        if (typeof window.UpdateVoiceProgress === "function") {
            window.UpdateVoiceProgress(current, total);
        }
    },

    JS_OnVoiceDownloadComplete: function () {
        if (typeof window.OnVoiceDownloadComplete === "function") {
            window.OnVoiceDownloadComplete();
        }
    },

    JS_Log_Dump: function (str) {
        // Dummy function biar Unity WebGL ga complain DllNotFoundException
    },

    ReloadIframe: function () {
        window.location.reload();
    },

    LogToBrowser: function (message) {
        console.log(UTF8ToString(message));
    },
});