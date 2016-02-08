(function (TableViewer, $, undefined) {
    // TableTileView is a UI control which displays the given TableViewer.Table as a collection of tiles (TableTile control),
    // where each tile corresponds to a single table column.
    TableViewer.showSummary = function (htmlElement, tableSource) {
        var table = new TableViewer.TableViewModel(tableSource);
        var element = $(htmlElement);
        var tiles = [];
        element
            .empty()
            .addClass("table-tileView")
            .bind('mouseenter.' + name, function () {
                mouseOverBox = true;
            })
            .bind('mouseleave.' + name, function () {
                mouseOverBox = false;
            });

        var panel = $("<div></div>")
            .appendTo(element)
            .addClass("table-tileView-panel");

        var panel_ul = $("<ul></ul>")
            .appendTo(panel);

        var _onColumnsChanged = function () {
            var ul = panel_ul;
            ul.empty();

            if (table && table.columns) {
                var columns = table.columns;
                var bindTile = function (tileDiv, column) {
                    tileDiv.bind("click", function (event) {
                        tileDiv.trigger("tileSelected", column.name);
                    });
                }
                tiles = [];
                for (var n = columns.length, i = 0; i < n; i++) {
                    var column = columns[i];
                    var li = $("<li></li>")
                        .appendTo(ul);
                    var tileDiv = $("<div></div>")
                        .appendTo(li)
                    var tile = TableViewer.TableTile(tileDiv, column);
                    tiles.push(tile);
                    bindTile(tileDiv, column);
                }
            }
        }

        table.onColumnsChanged = function (args) { if (args.changeType === 'schema') _onColumnsChanged(); }
        _onColumnsChanged();

        return {
            dispose: function () {
                tiles.forEach(function (t) { t.dispose(); });
                element
                    .empty()
                    .removeClass("table-tileView");
                table.onColumnsChanged = undefined;
                table = undefined;
                options = undefined;
            }
        };
    };
}(TableViewer, $));