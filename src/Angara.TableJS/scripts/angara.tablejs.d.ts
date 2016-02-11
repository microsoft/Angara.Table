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

    type RealColumnSummary = {
        /** Column name */
        name: string;
        /** Column element type */
        type: string; 
        /** Number of elements in the column */
        totalCount: number;
        /** Number of data elements in the column (ignoring NaN) */
        count: number;
        /** Maximum value of the column */
        max: number;
        /** Minimum value of the column */
        min: number;
        mean: number;
        median: number;
        variance: number;
        lb68: number;
        ub68: number;
        lb95: number;
        ub95: number;
    }
    
    type BooleanColumnSummary = {
        /** Column name */
        name: string;
        /** Column element type (always "bool") */
        type: string; 
        /** Number of true elements */
        true: number;
        /** Number of false elements */
        false: number;
    }
    
    type ComparableColumnSummary = {
        /** Column name */
        name: string;
        /** Column element type ("date" or "string") */
        type: string; 
        /** Number of elements in the column */
        totalCount: number;
        /** Number of data elements in the column (ignoring null strings) */
        count: number;
        /** Maximum value of the column */
        max: number;
        /** Minimum value of the column */
        min: number;
    }
    
    type ColumnSummary = RealColumnSummary | BooleanColumnSummary | ComparableColumnSummary;

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
    
    type TableDescription = {
        /** Number of rows in the table */
        count: number;
        /** Describes each column of the table */
        summary: ColumnSummary[];
        /** Contains data array for each column of the table */
        data: (number[] | Boolean[] | Date[] | String[])[];
        /** Contains probability density function (if applicable) or null (otherwise),
         * for each column of the table. */
        pdf: ColumnPdf[];
        /** Contains Pearson's correlations between all numeric columns of a table. */
        correlation: Correlation;
    }

    /** Represent a data source for the TableViewer control. 
     *  It is assumed that this type instance wraps a table and metadata collection. */
    interface TableSource {
        /** The callback must be called when either schema or data of the table is changed. */
        onChanged: TableChangedEventDelegate;
        /** Number of rows in the table */
        totalRows: number;
        /** Schema of columns */
        columns: ColumnDefinition[];
        /** Asynchronously request a part of all columns data arrays.
         *  Response is an array of data columns, where each data column is an array with an element type depending on the column type.*/
        getDataAsync(startRow: number, rows: number): JQueryPromise<any[][]>;
        getSummaryAsync(columnNumber: number): JQueryPromise<ColumnSummary>;
        getPdfAsync(columnNumber: number): JQueryPromise<ColumnPdf>;
        getCorrelationAsync(): JQueryPromise<Correlation>;
        saveAttribute(key: string, value: any);
        getAttributeAsync(key: string): JQueryPromise<any>;
        /** Rejects all in-progress requests of the instance (i.e. all incomplete get###() methods) */
        cancelRequests(): void;
    }

    interface TableSourceFactory {
        new (tableDescription: TableDescription): TableSource;
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
        /** Takes an object of specific schema and creates
        / * a table source object that can be used to create the table viewer.*/
        TableSource: TableSourceFactory;
    }
}