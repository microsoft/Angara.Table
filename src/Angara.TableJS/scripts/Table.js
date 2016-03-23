define(["jquery", "angara.tablejs", "exports"], function ($, TableJs, exports) {    
    exports.Show = function (tableSource, container) {
        TableViewer.show(container, tableSource);
    };
});
