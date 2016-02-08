(function (TableViewer) {
    var TableColumnViewModel = (function () {
        function TableColumnViewModel(number, name, type, table) {
            this.number = number;
            this.name = name;
            this.type = type;
            this.table = table;
        }
        TableColumnViewModel.prototype.getSummaryAsync = function () {
            return this.table.getSummaryAsync(this.number);
        };
        TableColumnViewModel.prototype.getPdfAsync = function () {
            return this.table.getPdfAsync(this.number);
        };
        return TableColumnViewModel;
    })();
    var TableViewModel = (function () {
        function TableViewModel(table) {
            this.table = table;
            this.columns = [];
            var that = this;
            table.onChanged = function (args) {
                if (args.changeType == "schema") {
                    that.updateColumns();
                }
                else {
                    that.columns.forEach(function (c) {
                        if (c.onChanged) c.onChanged();
                    });
                }
                if (that.onColumnsChanged) {
                    that.onColumnsChanged(args);
                }
            };
            this.updateColumns();
        }
        TableViewModel.prototype.updateColumns = function () {
            var that = this;
            this.columns = this.table.columns.map(function (colDef, index) {
                return new TableColumnViewModel(index, colDef.name, colDef.type, that);
            });
        };
        Object.defineProperty(TableViewModel.prototype, "totalRows", {
            get: function () {
                return this.table.totalRows;
            },
            enumerable: true,
            configurable: true
        });
        TableViewModel.prototype.getDataAsync = function (startRow, rows) {
            return this.table.getDataAsync(startRow, rows);
        };
        TableViewModel.prototype.getSummaryAsync = function (columnNumber) {
            return this.table.getSummaryAsync(columnNumber);
        };
        TableViewModel.prototype.getPdfAsync = function (columnNumber) {
            return this.table.getPdfAsync(columnNumber);
        };
        TableViewModel.prototype.getCorrelationAsync = function () {
            return this.table.getCorrelationAsync();
        };
        TableViewModel.prototype.saveAttribute = function (key, value) {
            this.table.saveAttribute(key, value);
        };
        TableViewModel.prototype.getAttribute = function (key) {
            return this.table.getAttributeAsync(key);
        };
        TableViewModel.prototype.saveView = function (value) {
            this.saveAttribute("$view", value);
        };
        TableViewModel.prototype.getView = function () {
            return this.getAttributeAsync("$view");
        };
        TableViewModel.prototype.cancelAllRequests = function () {
            this.table.cancelRequests();
        };
        return TableViewModel;
    })();
    TableViewer.TableViewModel = TableViewModel;
})(TableViewer);
