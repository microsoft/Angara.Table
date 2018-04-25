define(["jquery", "angara.tablejs", "exports"], function ($, TableViewer, exports) {    
    exports.Show = function (tableView, container) {
        tableSource = tableView["table"];
        tableSource.viewSettings =  tableView["viewSettings"];
        TableViewer.show(container, tableSource);
    };
});