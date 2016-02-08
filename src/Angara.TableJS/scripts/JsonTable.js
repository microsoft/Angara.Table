(function (TableViewer) {
    function JsonTable(totalRows, columns, summaries, pdf, dataByCols, correlation) {
        this.totalRows = totalRows;
        this.columns = columns;
        this.summaries = summaries;
        this.pdf = pdf;
        this.dataByCols = dataByCols;
        this.correlation = correlation;
        this.onChanged = undefined;
        this.metadata = {};
    }
    JsonTable.prototype.getDataAsync = function (startRow, rows) {
        var p = $.Deferred();
        var slices = this.dataByCols.map(function (col) {
            return Array.prototype.slice.apply(col, [startRow, startRow + rows - 1]);
        });
        p.resolve(slices);
        return p.promise();
    };
    JsonTable.prototype.getSummaryAsync = function (columnNumber) {
        var res = $.Deferred();
        res.resolve(this.summaries[columnNumber]);
        return res.promise();
    };
    JsonTable.prototype.getPdfAsync = function (columnNumber) {
        var res = $.Deferred();
        if (this.pdf && this.pdf.length > columnNumber)
            res.resolve(this.pdf[columnNumber]);
        else
            res.reject("no data");
        return res.promise();
    };
    JsonTable.prototype.getCorrelationAsync = function () {
        var res = $.Deferred();
        res.resolve(this.correlation);
        return res.promise();
    };
    JsonTable.prototype.saveAttribute = function (key, value) {
        this.metadata[key] = value;
    };
    JsonTable.prototype.getAttributeAsync = function (key) {
        var res = $.Deferred();
        res.resolve(this.metadata[key]);
        return res.promise();
    };
    JsonTable.prototype.cancelRequests = function () {
    };
    TableViewer.JsonTable = JsonTable;
})(TableViewer);
