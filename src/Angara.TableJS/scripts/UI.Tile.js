(function (TableViewer, $, undefined) {
    var typeToText = function (typeName) {
        if (typeName.indexOf("System.") == 0)
            typeName = typeName.substr(7);
        typeName = typeName.toLowerCase();
        if (typeName === "real") typeName = "float"; // F# notation
        else if (typeName === "int32") typeName = "int";
        return typeName;
    }

    TableViewer.TableTile = function(htmlElement, column /* TableColumnViewModel */) {
        var element = $(htmlElement);
        var chart= null;
        var bandPlot= null;
        var boxplot= null;
        var plotsBinding= null;

        InteractiveDataDisplay.Padding = 2;
        InteractiveDataDisplay.tickLength = 5;

        element
            .bind('mouseenter.' + name, function () { mouseOverBox = true; })
            .bind('mouseleave.' + name, function () { mouseOverBox = false; });

        element.html("<div class='table-tile'><div class='header'><div class='name'>Name</div><div class='rightContainer'><span class='type'></span></div></div>" +
                          "<div style='position: relative;'>" +
                          "<div class='content-chart' style='position: absolute'><div class='chart' data-idd-plot='figure'><div data-idd-axis='numeric' data-idd-placement='bottom' /></div>" +
                          "<div class='boxplot' data-idd-plot='boxplot' data-idd-style='stroke: rgb(89,150,255); thickness: 1'></div></div>" +
                          "<div class='content-summary' style='position: absolute'>" +
                          "<div class='summary-numeric'><span>min/max:</span>&nbsp;<span class='minmax'></span><span class='minmax-exp'></span></div><div class='summary-numeric'><span>lb95/ub95:</span>&nbsp;<span class='b95'>" +
                          "</span><span class='b95-exp'></span></div><div class='summary-numeric'><span>lb68/ub68:</span>&nbsp;<span class='b68'></span><span class='b68-exp'></span></div><div class='summary-numeric'><span>mean/std:</span>&nbsp;" +
                          "<span class='meanstd'></span><span class='meanstd-exp'></span></div><div class='summary-numeric'><span>median:</span>&nbsp;<span class='median'></span><span class='median-exp'></span></div><div class='summary-nonnumeric'></div>" +
                          "</div>" +
                          "</div></div>");

        var contentChart = element.find(".content-chart");
        var chart = InteractiveDataDisplay.asPlot(contentChart.find(".chart"));

        var d3Chart = chart;
        var div = $("<div></div>")
                  .attr("data-idd-name", "bplot")
                  .attr("data-idd-plot", "area")
                  .appendTo(d3Chart.host);
        var plot = new InteractiveDataDisplay.Area(div, d3Chart.master);
        d3Chart.addChild(plot);
        bandPlot = plot;

        boxplot = InteractiveDataDisplay.asPlot(contentChart.find(".boxplot"));

        chart.navigation.setVisibleRect({ x: 0, y: 0, width: 1, height: 1 });
        boxplot.navigation.setVisibleRect({ x: 0, y: -0.5, width: 1, height: 1 });

        chart.navigation.gestureSource = undefined;//InteractiveDataDisplay.Gestures.getGesturesStream(chart.host);

        var _drawSummary = function (summary) {
            var type = typeToText(column.type);
            if(column.type == "bool")
                type = type + "[" + (summary.true + summary.false) + "]";
            else
                type = type + "[" + (summary.totalCount === summary.count ? summary.totalCount : summary.totalCount + "<span class='notImportantText'>/" + summary.count + "</span>") + "]";
            element.find(".type").html(type);

            if(column.type == "bool"){
                $(".summary-numeric", element).css("display", "none");
                $(".summary-nonnumeric", element).css("display", "block");

                element.find(".summary-nonnumeric").html("<div>true:" + summary.true + "</div><div>false:" + summary.false + "</div>");
                if (chart.isVisible) {
                    boxplot.isVisible = false;
                    chart.isVisible = false;
                    contentChart.css("display", "none");
                }
            } else if (typeof (summary.min) === 'undefined') {
                $(".summary-numeric", element).css("display", "none");
                $(".summary-nonnumeric", element).css("display", "none");
                if (chart.isVisible) {
                    boxplot.isVisible = false;
                    chart.isVisible = false;
                    contentChart.css("display", "none");
                }
            } else if (typeof (summary.variance) === 'undefined') {
                $(".summary-numeric", element).css("display", "none");
                $(".summary-nonnumeric", element).css("display", "block");

                if (summary.totalCount == 1)
                    element.find(".summary-nonnumeric").html("<div>" + summary.min + "</div>");
                else if (summary.totalCount == 2)
                    element.find(".summary-nonnumeric").html("<div>" + summary.min + "</div><div>" + summary.max + "</div>");
                if (summary.totalCount > 2)
                    element.find(".summary-nonnumeric").html("<div>" + summary.min + "</div><div>...</div><div>" + summary.max + "</div>");
                if (chart.isVisible) {
                    boxplot.isVisible = false;
                    chart.isVisible = false;
                    contentChart.css("display", "none");
                }
            } else {
                $(".summary-numeric", element).css("display", "block");
                $(".summary-nonnumeric", element).css("display", "none");

                var std = Math.sqrt(summary.variance);
                var formatter = TableViewer.MathUtils.getPrintFormat(summary.min, summary.max, std);
                var f = formatter.toString;

                element.find(".minmax").html(f(summary.min) + "/" + f(summary.max));
                element.find(".meanstd").html(f(summary.mean) + "/" + f(std));
                element.find(".median").html(f(summary.median));
                element.find(".b68").html(f(summary.lb68) + "/" + f(summary.ub68));
                element.find(".b95").html(f(summary.lb95) + "/" + f(summary.ub95));

                if (formatter.exponent) {
                    element.find(".minmax-exp").html("×10<sup>" + formatter.exponent + "</sup>");
                    element.find(".meanstd-exp").html("×10<sup>" + formatter.exponent + "</sup>");
                    element.find(".median-exp").html("×10<sup>" + formatter.exponent + "</sup>");
                    element.find(".b68-exp").html("×10<sup>" + formatter.exponent + "</sup>");
                    element.find(".b95-exp").html("×10<sup>" + formatter.exponent + "</sup>");
                }

                var range = summary.max - summary.min;
                if (!chart.isVisible) {
                    contentChart.css("display", "block");
                    boxplot.isVisible = true;
                    chart.isVisible = true;
                } 
                var h = 15;
                boxplot.draw({
                    min: summary.min,
                    l95: summary.lb95,
                    l68: summary.lb68,
                    median: summary.median,
                    r68: summary.ub68,
                    r95: summary.ub95,
                    max: summary.max,
                    y: 0, height: h
                });

                if (range == 0) range = 1.0;
                var left = summary.min - range / 10;
                var right = summary.max + range / 10;

                var vr = chart.visibleRect;
                chart.navigation.setVisibleRect({ x: left, y: vr.y, width: right - left, height: vr.height });
                boxplot.navigation.setVisibleRect({ x: left, y: -0.5, width: right - left, height: 1 });
            }
        };

        var _drawPdf = function (pdf) {
            // it is called only after the _drawSummary which makes initial settings for the 'chart', too.
            if (pdf) {
                var drawArgs = {};
                drawArgs.fill = "lightblue";

                if (typeof (pdf.x) === 'undefined' || typeof (pdf.f) === 'undefined') {
                    drawArgs.y1 = [];
                    drawArgs.y2 = [];
                    drawArgs.x = [];
                    bandPlot.draw(drawArgs);
                }
                else {
                    var y1 = new Array(pdf.x.length);
                    for(var i=0; i < y1.length; i++) y1[i] = 0;
                    drawArgs.y1 = y1;
                    drawArgs.y2 = pdf.f;
                    drawArgs.x = pdf.x;
                    bandPlot.draw(drawArgs);
                }
                chart.fitToViewY();
            }
        }

        var _onChanged = function () {
            column.getSummaryAsync()
                .done(function (summary) { // updating summary
                    _drawSummary(summary);
                    column.getPdfAsync()
                        .done(function (pdf) { // updating summary
                            _drawPdf(pdf);
                        })
                        .fail(function (reason) {
                            if (reason != 'Task is canceled') {
                                // todo: show the error message
                            }
                        });
                })
                .fail(function (reason) {
                    if (reason != 'Task is canceled') {
                        // todo: show the error message
                    }
                });
        };

        column.onChanged = function () {
            _onChanged();
        }
        element.find(".name").text(column.name);
        element.find(".type").text(typeToText(column.type) + "[]");
        _onChanged();
        return {
            dispose: function () {
                element.empty();
                column.onChanged = undefined;
                column = undefined;
            }
        }
    };
}(TableViewer, $));
