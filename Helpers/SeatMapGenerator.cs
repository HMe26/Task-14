namespace CinemaApp.Helpers;

public static class SeatMapGenerator
{
    private const int COLUMNS_PER_ROW = 10;

    public static List<Seat> Generate(int movieId, int totalSeats)
    {
        var seats = new List<Seat>();

        for (int i = 0; i < totalSeats; i++)
        {
            int rowIndex = i / COLUMNS_PER_ROW;
            int columnNumber = (i % COLUMNS_PER_ROW) + 1;
            string rowLabel = ((char)('A' + rowIndex)).ToString();

            seats.Add(new Seat
            {
                MovieId = movieId,
                RowLabel = rowLabel,
                ColumnNumber = columnNumber,
                SeatNumber = $"{rowLabel}{columnNumber}",
            });
        }

        return seats;
    }
}
