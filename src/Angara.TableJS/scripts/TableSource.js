(function (TableViewer) {
    /** Takes an object of specific schema and creates
    / * a table source object that can be used to create the table viewer. */
    function TableSource(tableDescription) {
        var columns = [];
        for(var i = 0; i < tableDescription.summary.length; i++)
            columns.push({name: tableDescription.summary[i].name, type: tableDescription.summary[i].type});
        this.totalRows = tableDescription.summary && tableDescription.summary.length > 0 ? tableDescription.summary[0].totalCount : 0;
        this.columns = columns;        
        this.summary = tableDescription.summary;
        this.pdf = tableDescription.pdf;
        this.dataByCols = tableDescription.data;
        this.correlation = tableDescription.correlation;
        this.onChanged = undefined;
        this.metadata = {};
    }
    TableSource.prototype.getDataAsync = function (startRow, rows) {
        var p = $.Deferred();
        var slices = this.dataByCols.map(function (col) {
            return Array.prototype.slice.apply(col, [startRow, startRow + rows - 1]);
        });
        p.resolve(slices);
        return p.promise();
    };
    TableSource.prototype.getSummaryAsync = function (columnNumber) {
        var res = $.Deferred();
        res.resolve(this.summary[columnNumber]);
        return res.promise();
    };
    TableSource.prototype.getPdfAsync = function (columnNumber) {
        var res = $.Deferred();
        if (this.pdf && this.pdf.length > columnNumber)
            res.resolve(this.pdf[columnNumber]);
        else
            res.reject("no data");
        return res.promise();
    };
    TableSource.prototype.getCorrelationAsync = function () {
        var res = $.Deferred();
        res.resolve(this.correlation);
        return res.promise();
    };
    TableSource.prototype.saveAttribute = function (key, value) {
        this.metadata[key] = value;
    };
    TableSource.prototype.getAttributeAsync = function (key) {
        var res = $.Deferred();
        res.resolve(this.metadata[key]);
        return res.promise();
    };
    TableSource.prototype.cancelRequests = function () {
    };
    TableViewer.TableSource = TableSource;    
})(TableViewer);
