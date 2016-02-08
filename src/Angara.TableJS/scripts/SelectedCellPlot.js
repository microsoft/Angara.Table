(function (TableViewer, InteractiveDataDisplay, $, undefined) {
    TableViewer.SelectedCellPlot = function (jqDiv, master) {
        this.base = InteractiveDataDisplay.CanvasPlot;
        this.base(jqDiv, master);

        var x, y, n;

        this.draw = function (data) {
            this.x = data.x;
            this.y = data.y;
            this.n = data.n;
            this.requestNextFrameOrUpdate();
        }

        this.renderCore = function (plotRect, screenSize) {
            var context = this.getContext(true);
            if (this.y >= 0 && this.x >= 0) {
                context.strokeStyle = "black";

                var t = this.getTransform();
                var dataToScreenX = t.dataToScreenX;
                var dataToScreenY = t.dataToScreenY;
                var hs1 = t.dataToScreenY(this.y);
                var hs2 = t.dataToScreenY(this.y + 1);
                var ws1 = t.dataToScreenX(this.x);
                var ws2 = t.dataToScreenX(this.x + 1);
                context.strokeRect(ws1, hs1, ws2 - ws1, hs2 - hs1);

                var n = this.n;
                hs1 = t.dataToScreenY(n - this.x - 1);
                hs2 = t.dataToScreenY(n - this.x);
                ws1 = t.dataToScreenX(n - this.y - 1);
                ws2 = t.dataToScreenX(n - this.y);
                context.strokeRect(ws1, hs1, ws2 - ws1, hs2 - hs1);
            }
        }
    };

    TableViewer.SelectedCellPlot.prototype = new InteractiveDataDisplay.CanvasPlot;

    $(document).ready(function () {
        InteractiveDataDisplay.register('selectedCell', function (jqDiv, master) { return new TableViewer.SelectedCellPlot(jqDiv, master); });
    });
}(TableViewer, InteractiveDataDisplay, $));