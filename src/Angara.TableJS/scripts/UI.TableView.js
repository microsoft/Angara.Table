(function (TableViewer, $, undefined) {
    $.fn.scrollView = function () {
        return each(function () {
            $('html, body').animate({
                scrollLeft: $(this).offset().left
            }, 1000);
        });
    };
    $.fn.dataTableExt.oPagination.four_button = {
        "fnInit": function (oSettings, nPaging, fnCallbackDraw) {
            var nFirst = document.createElement('span');
            var nPrevious = document.createElement('span');
            var nNext = document.createElement('span');
            var nLast = document.createElement('span');

            nFirst.appendChild(document.createTextNode(oSettings.oLanguage.oPaginate.sFirst));
            nPrevious.appendChild(document.createTextNode(oSettings.oLanguage.oPaginate.sPrevious));
            nNext.appendChild(document.createTextNode(oSettings.oLanguage.oPaginate.sNext));
            nLast.appendChild(document.createTextNode(oSettings.oLanguage.oPaginate.sLast));

            nFirst.className = "paginate_button first";
            nPrevious.className = "paginate_button previous";
            nNext.className = "paginate_button next";
            nLast.className = "paginate_button last";

            nPaging.appendChild(nFirst);
            nPaging.appendChild(nPrevious);
            nPaging.appendChild(nNext);
            nPaging.appendChild(nLast);

            $(nFirst).click(function () {
                oSettings.oApi._fnPageChange(oSettings, "first");
                fnCallbackDraw(oSettings);
            });

            $(nPrevious).click(function () {
                oSettings.oApi._fnPageChange(oSettings, "previous");
                fnCallbackDraw(oSettings);
            });

            $(nNext).click(function () {
                oSettings.oApi._fnPageChange(oSettings, "next");
                fnCallbackDraw(oSettings);
            });

            $(nLast).click(function () {
                oSettings.oApi._fnPageChange(oSettings, "last");
                fnCallbackDraw(oSettings);
            });

            /* Disallow text selection */
            $(nFirst).bind('selectstart', function () { return false; });
            $(nPrevious).bind('selectstart', function () { return false; });
            $(nNext).bind('selectstart', function () { return false; });
            $(nLast).bind('selectstart', function () { return false; });
        },


        "fnUpdate": function (oSettings, fnCallbackDraw) {
            if (!oSettings.aanFeatures.p) {
                return;
            }

            /* Loop over each instance of the pager */
            var an = oSettings.aanFeatures.p;
            for (var i = 0, iLen = an.length ; i < iLen ; i++) {
                var buttons = an[i].getElementsByTagName('span');
                if (oSettings._iDisplayStart === 0) {
                    buttons[0].className = "paginate_disabled_previous";
                    buttons[1].className = "paginate_disabled_previous";
                }
                else {
                    buttons[0].className = "paginate_enabled_previous";
                    buttons[1].className = "paginate_enabled_previous";
                }

                if (oSettings.fnDisplayEnd() == oSettings.fnRecordsDisplay()) {
                    buttons[2].className = "paginate_disabled_next";
                    buttons[3].className = "paginate_disabled_next";
                }
                else {
                    buttons[2].className = "paginate_enabled_next";
                    buttons[3].className = "paginate_enabled_next";
                }
            }
        }
    };
    
    // TableTileView is a UI control which displays the given TableViewer.Table as a table.
    TableViewer.showGrid = function(htmlElement, tableSource, activeColumn) {
        var table = new TableViewer.TableViewModel(tableSource);
        var element = $(htmlElement);
        var tableui = null;
        var formatters = [];
        var isAutoFormatEnabled = true;

        element
            .empty()
            .addClass("table-tableView")
            .bind('mouseenter.' + name, function () {
                mouseOverBox = true;
            })
            .bind('mouseleave.' + name, function () {
                mouseOverBox = false;
            });

        var panel = $("<div></div>")
            .appendTo(element)
            .addClass("table-tableView-panel");

        var _getTableData = function (sSource, aoData, fnCallback, oSettings) {
            // method is called by jquery.datatables plugin
            // see http://datatables.net/ref#fnServerData
            // {string}: HTTP source to obtain the data from (sAjaxSource)
            // {array}: A key/value pair object containing the data to send to the server
            // {function}: to be called on completion of the data get process will draw the data on the page.
            // {object}: DataTables settings object
            var fnGetKey = function (aoData, sKey) {
                for (var i = 0, iLen = aoData.length ; i < iLen ; i++) {
                    if (aoData[i].name == sKey) {
                        return aoData[i].value;
                    }
                }
                return null;
            }

            var startRow = fnGetKey(aoData, "iDisplayStart");
            var rows = fnGetKey(aoData, "iDisplayLength");
            var sEcho = fnGetKey(aoData, "sEcho");
            
            table.getDataAsync(startRow, rows).done(function (result) {
                var data = result;
                var total = table.totalRows;
                var coldata = [];
                for (var i in data) {
                    coldata.push(data[i]);//Is typed array OK here? To the first glance it appears to be working
                }
                var n = coldata.length;//number of columns
                var m = coldata[n - 1].length; //number of rows

                var aaData = new Array(m);
                var response = {
                    sEcho: sEcho,
                    iTotalRecords: total,
                    iTotalDisplayRecords: total,
                    aaData: aaData
                };

                for (var k = 0; k < m; k++) { // rows
                    var row = new Array(n);
                    for (var col = 0; col < n; col++) { // columns
                        row[col] = TableViewer.MathUtils.escapeHtml(coldata[col][k]);
                    }
                    aaData[k] = row;
                }

                fnCallback(response);
            })
            .fail(function () {
                alert('table data request failed');
            });
        };

        var _enableAutoFormat = function () {
            if (!tableui) return;

            var round_btn = panel.find(".round_btn");
            round_btn[0].className = "round_btn auto_format_on";
            round_btn[0].title = "Auto format is ON";

            isAutoFormatEnabled = true;
            var _formatters = formatters;
            var setFormat = function (table, i, colName, tableui) {
                table.getSummaryAsync(i).done(function (summary) {
                    if (typeof (summary.min) !== 'undefined' && typeof (summary.max) !== 'undefined' && typeof (summary.variance) !== 'undefined') {
                        var formatter = TableViewer.MathUtils.getPrintFormat(summary.min, summary.max, Math.sqrt(summary.variance));
                        _formatters[i] = formatter;

                        var colHeader = $('th:nth-child(' + (i + 1) + ')', tableui);
                        if (colHeader.length) {
                            var title = _getColumnTitle(colName, formatter);
                            colHeader.html(title);
                        }
                        tableui.fnDraw(false);
                    }
                });
            };
            var n = table.columns.length;
            for (var i = 0; i < n; i++) {
                var colName = table.columns[i].name;
                var colType = table.columns[i].type;
                if (colType != "System.String" && colType != "System.DateTime")
                    setFormat(table, i, colName, tableui);
            }
        };

        var _getColumnTitle = function (columnName, formatter) {
            var title = $("<span></span>");
            $("<span></span>").appendTo(title).text(columnName);
            if (formatter && formatter.exponent) {
                $("<span class='headerExponent'>×10<sup><small>" + formatter.exponent + "</small></sup></span>").appendTo(title);
            }
            return title;
        };

        var _disableAutoFormat = function () {
            if (!tableui) return;

            var round_btn = panel.find(".round_btn");
            round_btn[0].className = "round_btn auto_format_off";
            round_btn[0].title = "Auto format is OFF";

            isAutoFormatEnabled = false;

            $('.headerExponent', tableui).remove();
            tableui.fnDraw(false);
        };

        var _onColumnsChanged = function (activeColumnName, changeType) {
            panel.empty();
            panel_table = $("<table cellpadding='0' cellspacing='0' border='0'></table>").appendTo(panel);

            var tbl = panel_table;

            if (table && table.columns && table.columns.length > 0) {
                var columns = table.columns;
                var thead = $("<thead></thead>").appendTo(tbl)
                var tr = $("<tr></tr>").appendTo(thead);

                var n = table.columns.length;
                var _formatters;
                if (changeType == 'schema' || !formatters)
                    _formatters = formatters = new Array(n);
                else
                    _formatters = formatters;

                var colDefs = new Array(n);
                var def = function (j, colName) {
                    colDefs[j] = {
                        sClass: activeColumnName && activeColumnName == colName ? "active" : undefined,
                        mData: function (source, type, val) {
                            if (type === "set") {
                                return;
                            } else if (type === "display") {
                                var val = source[j];
                                var formatter = _formatters[j]
                                if (isAutoFormatEnabled && val && formatter) {
                                    return formatter.toString(val);
                                }
                                return val;
                            }
                            return source[j];
                        }
                    };
                };

                for (var i = 0; i < n; i++) {
                    var formatter = _formatters[i]
                    var th = $("<th></th>").appendTo(tr);
                    var colName = columns[i].name;
                    var title = _getColumnTitle(colName, formatter);
                    th.html(title);

                    if (activeColumnName && activeColumnName == columns[i].name) {
                        th.scrollView();
                    }
                    def(i, columns[i].name);
                }
                var tbody = $("<tbody></tbody>").appendTo(tbl);

                tableui = tbl.dataTable({
                    bFilter: false,
                    bSort: false,
                    bAutoWidth: true,
                    bSortClasses: false,
                    bProcessing: true,
                    bServerSide: true,
                    sAjaxSource: "-na-",
                    sDom: 'l<"toolbar">fr<t><ip>',
                    sPaginationType: "four_button",
                    fnServerData: function (sSource, aoData, fnCallback, oSettings) {
                        _getTableData(sSource, aoData, fnCallback, oSettings);
                    },
                    bStateSave: true,
                    fnStateSave: function (oSettings, oData) {
                        var settings = {
                            "start": oData.iStart,
                            "length": oData.iLength
                        };
                        table.saveAttribute("$view-table", JSON.stringify(settings));
                    },
                    aoColumns: colDefs,
                    iDisplayLength: tableSettings.length || 10,
                    iDisplayStart: tableSettings.start || 0,
                    oLanguage: {
                        oPaginate: {
                            sFirst: "first",
                            sLast: "last",
                            sNext: "next",
                            sPrevious: "previous"
                        },
                        sEmptyTable: "no data available in table",
                        sInfo: "showing _START_ to _END_ of _TOTAL_ entries",
                        sInfoEmpty: "showing 0 to 0 of 0 entries",
                        sLengthMenu: "show _MENU_ entries",
                        sLoadingRecords: "loading...",
                        sProcessing: "processing..."
                    }
                });

                tbody.on("mouseenter", "td", function () {
                    var iCol = $('td', this.parentNode).index(this);
                    $('td:nth-child(' + (iCol + 1) + ')', tableui.$('tr')).addClass('highlighted');
                });
                tbody.on("mouseleave", "td", function () {
                    tableui.$('td.highlighted').removeClass('highlighted');
                });

                // Turn auto format on/off
                var round_btn = $("div.toolbar").html('<span class="round_btn"></span>').find(".round_btn");
                round_btn.css("background-size", "contain");
                round_btn.click(function (e) {
                    if (isAutoFormatEnabled)
                        _disableAutoFormat();
                    else
                        _enableAutoFormat();
                });
                if (isAutoFormatEnabled)
                    _enableAutoFormat();
                else
                    _disableAutoFormat();
            }
        };

        table.getAttribute("$view-table").done(function (value) {
            try {
                tableSettings = value ? JSON.parse(value) : {};
            } catch (x) {
                tableSettings = {};
            }
            table.onColumnsChanged = function (e) { _onColumnsChanged(null, e.changeType); }
            _onColumnsChanged(activeColumn, 'schema');
        });

        return { 
            dispose: function () {
                element
                    .empty()
                    .removeClass("table-tableView");
                table.onColumnsChanged = undefined;
                table = undefined;
                options = undefined;
            }   
        };
    }
}(TableViewer, $));
