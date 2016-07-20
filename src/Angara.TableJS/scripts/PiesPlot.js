(function (TableViewer, InteractiveDataDisplay, $, undefined) {
    TableViewer.PiesMarker = {
        prepare : function(data) {
            // y
            if(data.y == undefined || data.y == null) throw "The mandatory property 'y' is undefined or null";
            if(!InteractiveDataDisplay.Utils.isArray(data.y)) throw "The property 'y' must be an array of numbers";                
            var n = data.y.length;
            
            var mask = new Int8Array(n);
            InteractiveDataDisplay.Utils.maskNaN(mask, data.y);               
            
            // x
            if(data.x == undefined || data.x == null)  throw "The mandatory property 'x' is undefined or null";
            else if (!InteractiveDataDisplay.Utils.isArray(data.x)) throw "The property 'x' must be an array of numbers";  
            else if (data.x.length != n) throw "Length of the array which is a value of the property 'x' differs from lenght of 'y'"
            else InteractiveDataDisplay.Utils.maskNaN(mask, data.x);  

            if(InteractiveDataDisplay.Utils.isArray(data.color)) {
                if(data.color.length != n) throw "Length of the array 'color' is different than length of the array 'y'"            
                var palette = data.colorPalette;
                if (palette != undefined && palette.isNormalized) {
                    var r = InteractiveDataDisplay.Utils.getMinMax(data.color);
                    r = InteractiveDataDisplay.Utils.makeNonEqual(r);
                    data.colorPalette = palette = palette.absolute(r.min, r.max);
                }
                var colors = new Array(n);
                for (var i = 0; i < n; i++){
                    var color = data.color[i];
                    if(color != color) // NaN
                        mask[i] = 1;
                    else {
                        var rgba = palette.getRgba(color);                        
                        colors[i] = "rgba(" + rgba.r + "," + rgba.g + "," + rgba.b + "," + rgba.a + ")";
                    }
                }
                data.color = colors;
            }

            // Filtering out missing values
            var m = 0;
            for(var i = 0; i < n; i++) if(mask[i] === 1) m++;            
            if(m > 0){ // there are missing values
                m = n - m; 
                data.x = InteractiveDataDisplay.Utils.applyMask(mask, data.x, m);
                data.y = InteractiveDataDisplay.Utils.applyMask(mask, data.y, m);
                data.color = InteractiveDataDisplay.Utils.applyMask(mask, data.color, m);
            }
        },

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

        hitTest: function (marker, transform, ps, pd) {
            var r = marker.radius;
            return (pd.x - marker.x) * (pd.x - marker.x) + (pd.y - marker.y) * (pd.y - marker.y) <= r * r;
        }
    };
}(TableViewer, InteractiveDataDisplay, $));