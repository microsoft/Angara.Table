(function (TableViewer, InteractiveDataDisplay, $, undefined) {
    TableViewer.BoxPlot = function (jqDiv, master) {
        this.base = InteractiveDataDisplay.CanvasPlot;
        this.base(jqDiv, master);

        var _l95, _r95, _l68, _r68, _median, _min, _max;
        var _height;
        var _thickness = 1;
        var _stroke = 'black';

        this.draw = function (data) {
            _l95 = data.l95;
            _r95 = data.r95;
            _l68 = data.l68;
            _r68 = data.r68;
            _median = data.median;
            _min = data.min;
            _max = data.max;
            _height = data.height;

            this.invalidateLocalBounds();

            this.requestNextFrameOrUpdate();
            this.fireAppearanceChanged();
        };

        // Returns a rectangle in the plot plane.
        this.computeLocalBounds = function () {
            return undefined;
        };

        // Returns 4 margins in the screen coordinate system
        this.getLocalPadding = function () {
            var padding = 0;
            return { left: padding, right: padding, top: padding, bottom: padding };
        };

        this.renderCore = function (plotRect, screenSize) {
            var t = this.getTransform();
            var dataToScreenX = t.dataToScreenX;
            var dataToScreenY = t.dataToScreenY;

            var context = this.getContext(true);
            context.beginPath();
            context.strokeStyle = _stroke;

            // Horizontal line
            var xmin_s = dataToScreenX(_min);
            var xmax_s = dataToScreenX(_max);
            var xl68_s = dataToScreenX(_l68);
            var xl95_s = dataToScreenX(_l95);
            var xr68_s = dataToScreenX(_r68);
            var xr95_s = dataToScreenX(_r95);
            var xmedian_x = dataToScreenX(_median);

            var yc_s = dataToScreenY(0);
            var yt_s = yc_s - _height / 2;
            var yb_s = yc_s + _height / 2;

            var yt2_s = yc_s - _height / 6;
            var yb2_s = yc_s + _height / 6;

            var rad = 5;

            context.moveTo(xl95_s, yc_s);
            context.lineTo(xl68_s, yc_s);
            context.moveTo(xr95_s, yc_s);
            context.lineTo(xr68_s, yc_s);

            context.moveTo(xmedian_x, yt_s);
            context.lineTo(xmedian_x, yb_s);

            context.moveTo(xl95_s, yt2_s);
            context.lineTo(xl95_s, yb2_s);

            context.moveTo(xr95_s, yt2_s);
            context.lineTo(xr95_s, yb2_s);
            context.stroke();

            context.strokeRect(xl68_s, yt_s, xr68_s - xl68_s, _height);

            context.beginPath();
            context.arc(xmin_s, yc_s, rad, 0, 2 * Math.PI);
            context.stroke();
            context.beginPath();
            context.arc(xmax_s, yc_s, rad, 0, 2 * Math.PI);
            context.stroke();
        }
    };
    TableViewer.BoxPlot.prototype = new InteractiveDataDisplay.CanvasPlot;

    InteractiveDataDisplay.register('boxplot', function (jqDiv, master) { return new TableViewer.BoxPlot(jqDiv, master); });
}(TableViewer, InteractiveDataDisplay, $));