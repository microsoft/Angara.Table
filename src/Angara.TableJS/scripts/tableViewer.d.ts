declare module TableViewer {
    type JQueryPromise<T> = any;

    type ColumnDefinition = {
        name: string;
        type: string;
    }

    type TableChangedEventArgs = {
        /** Possible values are 'data' or 'schema'.
            If 'data' each column raised onChanged event,
            if 'schema' new columns were created.
         */
        changeType: string;
    }

    type TableChangedEventDelegate = (args: TableChangedEventArgs) => void;

    type ColumnSummary = {
        name: string;
        type: string;
        totalCount: number;
        count: number;
        max: number;
        min: number;
        mean: number;
        median: number;
        variance: number;
        lb68: number;
        ub68: number;
        lb95: number;
        ub95: number;
    }

    /** Contains probability density function computed for a table column */
    type ColumnPdf = {
        f: number[];
        x: number[];
    }

    /** Contains Pearson's correlations between all numeric columns of a table. */
    type Correlation = {
        /** Names of columns for which the correlations are computed (in original order) */
        c: string[];

        /** Array of Arrays of Pearson's correlations.
          * Each Array has correlation values of a column with same index with other columns of less index */
        r: number[][];
    }

    /** Represent a data source for the TableViewer control. 
     *  It is assumed that this type instance wraps a table and metadata collection.
     */
    interface TableSource {
        /** The callback must be called when either schema or data of the table is changed. */
        onChanged: TableChangedEventDelegate;

        /** Number of rows in the table */
        totalRows: number;

        /** Schema of columns */
        columns: ColumnDefinition[];

        /** Asynchronously requests data.
         *  Response is an array of data columns, where each data column is an array with an element type depending on the column type.
         */
        getDataAsync(startRow: number, rows: number): JQueryPromise<any[][]>;
        getSummaryAsync(columnNumber: number): JQueryPromise<ColumnSummary>;
        getPdfAsync(columnNumber: number): JQueryPromise<ColumnPdf>;
        getCorrelationAsync(): JQueryPromise<Correlation>;

        /**
         */
        saveAttribute(key: string, value: any);
        getAttributeAsync(key: string): JQueryPromise<any>;

        /** Rejects all in-progress requests of the instance (i.e. all incomplete get###() methods) */
        cancelRequests(): void;
    }

    interface JsonTableFactory {
        new (totalRows: number, columns: TableViewer.ColumnDefinition[], summaries: TableViewer.ColumnSummary[], pdf: TableViewer.ColumnPdf[], dataByCols: any[][], correlation: TableViewer.Correlation): TableSource;
    }

    interface Viewer {
        dispose(): void;
    }

    interface Factory {
        /** Shows the table using the given HTML element as a summary, grid and a correlation matrix. */
        show(domElement: HTMLElement, tableSource: TableSource): Viewer;
        /** Shows the table columns as a collection of tiles with summary.  */
        showSummary(domElement: HTMLElement, tableSource: TableSource): Viewer;
        /** Shows the table as a grid of columns and rows.  */
        showGrid(domElement: HTMLElement, tableSource: TableSource): Viewer;
        /** Shows the table as a grid of columns and rows, and highlights one of the columns.  */
        showGrid(domElement: HTMLElement, tableSource: TableSource, activeColumnName: string): Viewer;
        /** Shows the table as a matrix of correlations between numeric columns.  */
        showCorrelations(domElement: HTMLElement, tableSource: TableSource): Viewer;
        /** Creates an instance of a TableSource using Json objects. */
        JsonTable: JsonTableFactory;
    }
}