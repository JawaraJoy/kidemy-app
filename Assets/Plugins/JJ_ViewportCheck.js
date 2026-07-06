mergeInto(LibraryManager.library, {
    GetBrowserWidth: function () {
        return window.innerWidth || document.documentElement.clientWidth || document.body.clientWidth;
    },
    GetBrowserHeight: function () {
        return window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;
    }
});