define(["jquery", "angara.tablejs", "exports"], function ($, TableViewer, exports) {    
    exports.Show = function (tableSource, container) {
        tableSource.viewSettings =  { defaultTab: "summary", defaultPageSize: 10, hideNaNs: false };
        TableViewer.show(container, tableSource);
    };
});
