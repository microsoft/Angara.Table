(function (TableViewer, InteractiveDataDisplay, $, undefined) {
    TableViewer.PiesMarker = {
        // marker:
        // x, y is a center of a circle
        // radius 
        // value (between -1 and 1) -> angle
        // color
        // colorPalette
        draw: function (marker, plotRect, screenSize, transform, context) {
            var xs = transform.dataToScreenX(marker.x);
            var ys = transform.dataToScreenY(marker.y);
            var rs = transform.dataToScreenX(marker.x + marker.radius) - xs;
            var value = marker.value;

            if (value == 0 || xs + rs < 0 || xs - rs > screenSize.width || ys + rs < 0 || ys - rs > screenSize.height)
                return;

            context.beginPath();
            context.strokeStyle = "gray";
            context.fillStyle = marker.color;
            context.moveTo(xs, ys);
            context.lineTo(xs, ys - rs);
            context.arc(xs, ys, rs, -Math.PI / 2, Math.PI * (2 * value - 0.5), value < 0);
            context.lineTo(xs, ys);
            context.closePath();
            context.fill();
            context.stroke();

            context.beginPath();
            context.arc(xs, ys, rs, 0, 2 * Math.PI);
            context.stroke();
        },

        getBoundingBox: function (marker) {
            var r = marker.radius;
            var xLeft = marker.x - r;
            var yBottom = marker.y - r;
            return { x: xLeft, y: yBottom, width: barWidth, height: Math.abs(marker.y) };
        },

        hitTest: function (marker, transform, ps, pd) {
            var r = marker.radius;
            return (pd.x - marker.x) * (pd.x - marker.x) + (pd.y - marker.y) * (pd.y - marker.y) <= r * r;
        }
    };
}(TableViewer, InteractiveDataDisplay, $));