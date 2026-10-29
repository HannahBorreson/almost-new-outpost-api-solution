using MySql.Data.MySqlClient;

namespace AlmostNewOutpost.Api.Data;

/// <summary>
/// The MYSQL_ERRNO values our stored procedures raise with SIGNAL, plus MySQL's own
/// duplicate-key error. Services use these to turn a database error into a clear result.
/// </summary>
public static class DatabaseErrors
{
    public const int ProductNotFound = 50001;
    public const int CategoryNotFound = 50002;
    public const int DuplicateProductName = 50003;
    public const int ProductHasPurchases = 50004;
    public const int UserNotFound = 50005;
    public const int InvalidPrice = 50006;
    public const int InvalidSortBy = 50010;
    public const int InvalidSortDir = 50011;

    /// <summary>MySQL's built-in "duplicate entry for unique key" error.</summary>
    public const int DuplicateKey = 1062;

    /// <summary>
    /// Finds the MySqlException (it may be wrapped inside other exceptions) and returns its
    /// error number, or null when the error didn't come from MySQL.
    /// </summary>
    public static int? GetErrorNumber(Exception exception)
    {
        Exception? current = exception;
        while (current != null)
        {
            if (current is MySqlException mySqlException)
            {
                return mySqlException.Number;
            }

            current = current.InnerException;
        }

        return null;
    }
}
