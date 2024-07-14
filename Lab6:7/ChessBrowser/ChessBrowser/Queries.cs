using Microsoft.Maui.Controls;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessBrowser
{
  internal class Queries
  {

    /// <summary>
    /// This function runs when the upload button is pressed.
    /// Given a filename, parses the PGN file, and uploads
    /// each chess game to the user's database.
    /// </summary>
    /// <param name="PGNfilename">The path to the PGN file</param>
    internal static async Task InsertGameData( string PGNfilename, MainPage mainPage )
    {
     string connection = mainPage.GetConnectionString();
            List<ChessGame> games = PgnReader.LoadPgnFile(PGNfilename);

            mainPage.SetNumWorkItems(games.Count);

            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                try
                {
                    conn.Open();

                    foreach (var game in games)
                    {
                        using (var cmd = new MySqlCommand())
                        {
                            cmd.Connection = conn;
                            cmd.CommandText = @"
                                INSERT INTO Events (Event, Site, Round, EventDate) 
                                VALUES (@Event, @Site, @Round, @EventDate)
                                ON DUPLICATE KEY UPDATE Event=Event;

                                INSERT INTO Players (Name, Elo) 
                                VALUES (@White, @WhiteElo)
                                ON DUPLICATE KEY UPDATE Elo = GREATEST(Elo, @WhiteElo);

                                INSERT INTO Players (Name, Elo) 
                                VALUES (@Black, @BlackElo)
                                ON DUPLICATE KEY UPDATE Elo = GREATEST(Elo, @BlackElo);

                                INSERT INTO Games (Event, Site, Round, White, Black, WhiteElo, BlackElo, Result, Moves, EventDate)
                                VALUES (@Event, @Site, @Round, @White, @Black, @WhiteElo, @BlackElo, @Result, @Moves, @EventDate);";

                            cmd.Parameters.AddWithValue("@Event", game.Event);
                            cmd.Parameters.AddWithValue("@Site", game.Site);
                            cmd.Parameters.AddWithValue("@Round", game.Round);
                            cmd.Parameters.AddWithValue("@EventDate", game.EventDate);
                            cmd.Parameters.AddWithValue("@White", game.White);
                            cmd.Parameters.AddWithValue("@Black", game.Black);
                            cmd.Parameters.AddWithValue("@WhiteElo", game.WhiteElo);
                            cmd.Parameters.AddWithValue("@BlackElo", game.BlackElo);
                            cmd.Parameters.AddWithValue("@Result", game.Result);
                            cmd.Parameters.AddWithValue("@Moves", game.Moves);

                            await cmd.ExecuteNonQueryAsync();
                            await mainPage.NotifyWorkItemCompleted();
                        }
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
        }
    }



    /// <summary>
    /// Queries the database for games that match all the given filters.
    /// The filters are taken from the various controls in the GUI.
    /// </summary>
    /// <param name="white">The white player, or null if none</param>
    /// <param name="black">The black player, or null if none</param>
    /// <param name="opening">The first move, e.g. "1.e4", or null if none</param>
    /// <param name="winner">The winner as "W", "B", "D", or null if none</param>
    /// <param name="useDate">True if the filter includes a date range, False otherwise</param>
    /// <param name="start">The start of the date range</param>
    /// <param name="end">The end of the date range</param>
    /// <param name="showMoves">True if the returned data should include the PGN moves</param>
    /// <returns>A string separated by newlines containing the filtered games</returns>
    internal static string PerformQuery( string white, string black, string opening,
      string winner, bool useDate, DateTime start, DateTime end, bool showMoves,
      MainPage mainPage )
    {
     string connection = mainPage.GetConnectionString();
    string parsedResult = "";
    int numRows = 0;

    using (MySqlConnection conn = new MySqlConnection(connection))
    {
        try
        {
            conn.Open();
            using (var cmd = new MySqlCommand())
            {
                cmd.Connection = conn;
                var query = new StringBuilder();
                query.Append("SELECT Event, Site, EventDate, White, Black, WhiteElo, BlackElo, Result");
                if (showMoves)
                {
                    query.Append(", Moves");
                }
                query.Append(" FROM Games WHERE 1=1");

                if (!string.IsNullOrEmpty(white))
                {
                    query.Append(" AND White = @White");
                    cmd.Parameters.AddWithValue("@White", white);
                }
                if (!string.IsNullOrEmpty(black))
                {
                    query.Append(" AND Black = @Black");
                    cmd.Parameters.AddWithValue("@Black", black);
                }
                if (!string.IsNullOrEmpty(opening))
                {
                    query.Append(" AND Moves LIKE @Opening");
                    cmd.Parameters.AddWithValue("@Opening", opening + "%");
                }
                if (!string.IsNullOrEmpty(winner))
                {
                    query.Append(" AND Result = @Winner");
                    cmd.Parameters.AddWithValue("@Winner", winner);
                }
                if (useDate)
                {
                    query.Append(" AND EventDate BETWEEN @Start AND @End");
                    cmd.Parameters.AddWithValue("@Start", start);
                    cmd.Parameters.AddWithValue("@End", end);
                }

                cmd.CommandText = query.ToString();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        numRows++;
                        parsedResult += $"Event: {reader["Event"]}\n";
                        parsedResult += $"Site: {reader["Site"]}\n";
                        parsedResult += $"Date: {reader["EventDate"]}\n";
                        parsedResult += $"White: {reader["White"]} ({reader["WhiteElo"]})\n";
                        parsedResult += $"Black: {reader["Black"]} ({reader["BlackElo"]})\n";
                        parsedResult += $"Result: {reader["Result"]}\n";
                        if (showMoves)
                        {
                            parsedResult += $"Moves: {reader["Moves"]}\n";
                        }
                        parsedResult += "\n";
                    }
                }
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e.Message);
        }
    }

    return numRows + " results\n" + parsedResult;
}
}


