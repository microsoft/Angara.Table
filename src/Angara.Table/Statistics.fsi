module Angara.Data.TableStatistics

open System

/// Basic statistics for columns containing numeric data
type RealColumnSummary = {
    Min: float
    /// Lower bound of 95-th percentile.
    Lb95: float
    /// Lower bound of 68-th percentile.
    Lb68: float
    Median: float
    /// Upper bound of 68-th percentile.
    Ub68: float
    /// Upper bound of 95-th percentile.
    Ub95: float
    Max: float
    Mean: float
    Variance: float
    /// Total number of elements in the column.
    TotalCount: int
    /// Number of elements in the column except for NaNs.
    Count: int
}

/// Simple statistics for columns containing non-numeric data
type ComparableColumnSummary<'a when 'a : comparison> = {
    /// A minimum value of the column.
    Min: 'a
    /// A maximum value of the column.
    Max: 'a
    /// Total number of elements in the column.
    TotalCount: int
    /// Number of elements in the column except for missing values,
    /// which is null or empty string.
    Count: int
}

/// Simple statistics for columns containing boolean values.
type BooleanColumnSummary = {
    /// Number of rows with value "true"
    TrueCount: int
    /// Number of rows with value "false"
    FalseCount: int
}

/// Holds statistics for each column type
type ColumnSummary =
    /// Statistics for int and real columns
    | NumericColumnSummary of RealColumnSummary
    /// Statistics for string columns
    | StringColumnSummary of ComparableColumnSummary<string>
    /// Statistics for DateTime columns
    | DateColumnSummary of ComparableColumnSummary<DateTime>
    /// Statistics for Boolean columns
    | BooleanColumnSummary of BooleanColumnSummary

/// If at least two of the columns are real or int then Some(Column Names * Correlations)
/// else None
val TryCorrelation : table:Table -> (string[] * float[][]) option

/// If at least two of the table columns are real or int then returns (Column Names) * (Correlations)
/// else throws an exception.
val Correlation : table:Table -> (string[] * float[][])

/// Returns some simple statistical properties of a column.
val Summary : column:Column -> ColumnSummary

/// Tries to compute a probability density function of the column if the column is numeric.
val TryPdf : pointCount:int -> column:Column -> (float[] * float[]) option

/// Returns a probability density function of the column if the column is numeric.
val Pdf : pointCount:int -> column:Column -> (float[] * float[])