define(["jquery", "angara.tablejs", "exports"], function ($, TableViewer, exports) {    
    exports.Show = function (tableSource, container) {
        TableViewer.show(container, tableSource);
    };
});
