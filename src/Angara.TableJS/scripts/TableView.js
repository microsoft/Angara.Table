define(["jquery", "angara.tablejs", "exports"], function ($, TableViewer, exports) {    
    exports.Show = function (tableView, container) {
        var tableSource = tableView["table"];
        var viewSettings = tableView["viewSettings"];
        if (viewSettings.customFormatters) {
            Object.keys(viewSettings.customFormatters).forEach(function (key) {
                var ftext = viewSettings.customFormatters[key];
                viewSettings.customFormatters[key] = new Function('x', ftext);
            });
        } else
            viewSettings.customFormatters = {};
        tableSource.viewSettings = viewSettings;
        TableViewer.show(container, tableSource);
    };
});