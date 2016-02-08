(function (TableViewer, $, undefined) {
    $.fn.scrollView = function () {
        return this.each(function () {
            $('html, body').animate({
                scrollLeft: $(this).offset().left
            }, 1000);
        });
    }

    TableViewer.showCorrelations = function (htmlElement /*HTMLElement*/, tableSource /*TableSource*/) {
        var table = new TableViewer.TableViewModel(tableSource);
        var element = $(htmlElement);
        var axisLeft = null;
        var axisTop = null;
        var varsN = 0; // number of variables in the matrix (can be less than number of columns)
        var pcc = null;
        var selectedCellIndex = { i: -1, j: -1, isInverted: false };
        var colorPalette = InteractiveDataDisplay.ColorPalette.parse("-1=Red,White=0,Blue=1");
        var reqId = 0; // allows to order async getCorrelation responses

        element.empty().addClass("table-correlationView");
        var correlationViewContainer = $("<div style='display:none'></div>");
        element.append(correlationViewContainer);
        var msgDiv = $("<div class='message'></div>");
        element.append(msgDiv);

        var figureDiv = $("<div class='figure' data-idd-plot='figure'></div>");
        figureDiv.html('<div data-idd-plot="heatmap" data-idd-name="Pearson&#39;s correlation" data-idd-style="palette:-1=Red,White=0,Blue=1"></div><div data-idd-plot="grid" data-idd-name="grid" data-idd-placement="center" data-idd-style="stroke: DarkGray; thickness: 1px"></div><div data-idd-plot="selectedCell" data-idd-name="selectedCell" data-idd-placement="center"></div><div data-idd-plot="markers" data-idd-name="pies" data-idd-placement="center"></div>');
        correlationViewContainer.append(figureDiv);
        var figure = InteractiveDataDisplay.asPlot(figureDiv);
        figure.isToolTipEnabled = false;
        figure.navigation.gestureSource = InteractiveDataDisplay.Gestures.getGesturesStream(figureDiv);
            
        var legendDiv = $("<div class='legend'></div>")
        correlationViewContainer.append(legendDiv);
        var heatmap = figure.get("Pearson's correlation");
        heatmap.legend = new InteractiveDataDisplay.Legend(heatmap, legendDiv);

        correlationViewContainer.append($("<div class='legendHint hint'>click on a cell to see details here</div><div class='selectedPair' style='display:none;'>Pearson's correlation coefficient between "+
                "<span class='selectedPair1 highlighted'></span> and <span class='selectedPair2 highlighted'></span> is <span class='selectedPair-PCC highlighted'></span>.</div>"));
                        
        var _showDetailsFor = function (i, j, isInverted) {
            selectedCellIndex = { i: i, j: j, isInverted: isInverted };
            var selectedCell = figure.get("selectedCell");
            if (i >= 0 && j >= 0 && table && pcc && varsN > 0) {
                var n = varsN;
                if (isInverted) j = n - 1 - j;
                if (i != j && i < n && j < n && i >= 0 && j >= 0) {
                    if (isInverted)
                        selectedCell.draw({ x: i, y: n - 1 - j, n: n });
                    else
                        selectedCell.draw({ x: i, y: j, n: n });
                    var varX = table.columns[i].name;
                    var varY = table.columns[j].name;
                    element.find(".selectedPair2").text(varX);
                    element.find(".selectedPair1").text(varY);
                    if (i > j) { var l = i; i = j; j = l; }
                    element.find(".selectedPair-PCC").text(pcc[i][j - i - 1].toFixed(4));
                    element.find(".selectedPair").css("display", "block");
                    element.find(".legendHint").css("display", "none");
                    return;
                }
            }
            element.find(".selectedPair").css("display", "none");
            element.find(".legendHint").css("display", "block");
            selectedCell.draw({ x: -1, y: -1 });
        };

        var _onColumnsChanged = function (isSchemaChanged) {
            pcc = null; // obsolete data
            var cols = table.columns;
            if (cols && cols.length > 0) {
                var myReqId = ++reqId;
                msgDiv.css("display", "block").text("processing...").addClass("hint").removeClass("error");
                table.getCorrelationAsync()
                    .done(function (result) {
                        if (myReqId != reqId) return; // obsolete response
                        if (!result || !result.r || !result.c) {
                            varsN = 0;
                            correlationViewContainer.css("display", "none");
                            msgDiv.css("display", "block").text("there is nothing to display").addClass("hint").removeClass("error");
                            return;
                        }
                        msgDiv.css("display", "none");
                        var r = result.r;
                        pcc = result.r;
                        var heatmap = figure.get("Pearson's correlation");
                        var pies = figure.get("pies");
                        var n = Object.keys(r).length + 1 //r.length + 1;
                        varsN = n;
                        if (n < 2) {
                            correlationViewContainer.css("display", "none");
                        } else {
                            correlationViewContainer.css("display", "block");

                            var x = new Array(n + 1);
                            var f = new Array(n);
                            var xp = new Array(n * (n - 1) / 2);
                            var yp = new Array(n * (n - 1) / 2);
                            var fp = new Array(n * (n - 1) / 2);
                            for (var i = 0; i < n; i++) {
                                var fi = f[i] = new Array(n);
                                var ri = r[i];
                                for (var j = 0; j < n; j++) {
                                    if (i == j) fi[n - 1 - j] = 0;
                                    else if (i < j) fi[n - 1 - j] = 0;
                                    else fi[n - 1 - j] = r[j][i - j - 1];

                                    if (i < j) {
                                        fp[i * n + j] = ri[j - i - 1];
                                        xp[i * n + j] = i + 0.5;
                                        yp[i * n + j] = n - 1 - j + 0.5;
                                    }
                                }
                            }
                            for (var i = 0; i <= n; i++) {
                                x[i] = i;
                            }

                            heatmap.draw({ x: x, y: x, f: f });
                            pies.draw({ x: xp, y: yp, value: fp, color: fp, colorPalette: colorPalette, radius: 0.45, shape: TableViewer.PiesMarker });

                            if (isSchemaChanged) {
                                var labels = new Array(n);
                                var labelsHor = new Array(n);
                                    
                                for (var i = 0; i < n; i++) {
                                    var colName = TableViewer.MathUtils.escapeHtml(result.c[i]);
                                    labels[i] = "<div class='vert-axis-item' title='" + colName + "'>" + colName + "</div>";
                                    labelsHor[i] = "<div class='horz-axis-item-container'><div class='horz-axis-item' title='" + colName + "'>" + colName + "</div></div>";
                                }

                                if (axisLeft) axisLeft.axis.remove();
                                axisLeft = figure.addAxis("left", "labels",
                                    { labels: labels, ticks: x, rotate: false });

                                if (axisTop) axisTop.axis.remove();
                                axisTop = figure.addAxis("top", "labels",
                                    { labels: labelsHor, ticks: x, rotate: false });

                                var grid = figure.get("grid");
                                grid.xAxis = axisTop.axis;
                                grid.yAxis = axisLeft.axis;
                                axisLeft.axis.dataTransform = new InteractiveDataDisplay.DataTransform(
                                            function (x) { return n - x; },
                                            function (y) { return n - y; });

                                _showDetailsFor(-1, -1);
                                figure.fitToView();
                            } else {
                                _showDetailsFor(selectedCellIndex.i, selectedCellIndex.j, selectedCellIndex.isInverted);
                            }
                        } // n >= 2
                    })
                    .fail(function (err) {
                        varsN = 0;
                        correlationViewContainer.css("display", "none");
                        msgDiv.css("display", "block").text("Error: " + err).addClass("error").removeClass("hint");
                    });
            }
        }


        table.onColumnsChanged = function (args) { _onColumnsChanged(args.changeType === 'schema'); }
        _onColumnsChanged(true);

        heatmap.onClick = function (origin_s, origin_p) {
            _showDetailsFor(Math.floor(origin_p.x), Math.floor(origin_p.y), true);
        };

        return { dispose: function () {
            element
                .empty()
                .removeClass("table-correlationView");
            table.onColumnsChanged = undefined;
            figure = undefined;
            options = undefined;
        }};
    };
}(TableViewer, $));
