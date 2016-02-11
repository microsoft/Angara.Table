(function (TableViewer, $) {
    TableViewer.show = function (htmlElement /*HTMLElement*/, content /*TableSource or TableDescription*/) {
        var tableSource;
        if(typeof content["getDataAsync"] !== "undefined") // TableSource
            tableSource = content;
        else // TableDescription
            tableSource = new TableViewer.TableSource(content);
        
        var jqDiv = $(htmlElement);
        jqDiv.html("<div style='margin-top: 10px; border-bottom: 1px solid #808080; padding-bottom: 10px;'><span class='summary titleCommand-on'>summary</span>" +
                   "<span class='data titleCommand'>data</span><span class='correlation titleCommand'>correlation</span></div><div class='tableviewer_container'>Initializing...</div>");
        var container = jqDiv.find(".tableviewer_container");
        jqDiv.find(".summary").click(function () { showSummary() });
        jqDiv.find(".data").click(function () { showData() });
        jqDiv.find(".correlation").click(function () { showCorrelation() });

        var _activePage = undefined; // "summary", "data", or "correlation" (must be equal to id of the button elements)
        var _destroyActiveControl;
        var switchUI = function () {
            if (_destroyActiveControl) {
                _destroyActiveControl();
                _destroyActiveControl = undefined;
            }

            var commands = jqDiv.find(".titleCommand, .titleCommand-on");
            var on = commands.filter("." + _activePage);
            var off = commands.filter(":not(." + _activePage + ")");
            on.addClass("titleCommand-on").removeClass("titleCommand");
            off.addClass("titleCommand").removeClass("titleCommand-on");
        }
        var showSummary = function () {
            if (_activePage != "summary") {
                _activePage = "summary";
                switchUI();
                showTileView();
            }
        }
        var showData = function (activeColumn) {
            if (_activePage != "data") {
                _activePage = "data";
                switchUI();
                showTableView(activeColumn);
            }
        }
        var showCorrelation = function () {
            if (_activePage != "correlation") {
                _activePage = "correlation";
                switchUI();
                showCorrelationView();
            }
        }
        var showTileView = function () {
            tableSource.cancelRequests();
            var control = TableViewer.showSummary(container, tableSource);
            tableSource.saveAttribute("$view", "tiles");
            _destroyActiveControl = function () {
                if (container.is('.table-tileView')) control.dispose();
            };
            container.on("tileSelected", function (event, columnName) {
                showData(columnName);
            });
        }
        var showTableView = function (columnName) {
            tableSource.cancelRequests();
            var control = TableViewer.showGrid(container, tableSource, columnName);
            tableSource.saveAttribute("$view", "table");
            _destroyActiveControl = function () {
                if (container.is('.table-tableView')) control.dispose();
            };
        }
        var showCorrelationView = function () {
            tableSource.cancelRequests();
            var control = TableViewer.showCorrelations(container, tableSource);
            tableSource.saveAttribute("$view", "correlation");
            _destroyActiveControl = function () {
                if (container.is('.table-correlationView')) control.dispose();
            };
        }
        showSummary();
        return {
            dispose: function () {
                if (_destroyActiveControl) _destroyActiveControl();
                jqDiv.html("");
            }
        };
    }
}(TableViewer, $));
