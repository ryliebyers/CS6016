using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public static class PgnReader
{
    public static List<ChessGame> ReadPgnFile(string filePath)
    {
        List<ChessGame> games = new List<ChessGame>();
        string[] lines = File.ReadAllLines(filePath);
        ChessGame currentGame = null;
        string moves = string.Empty;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (currentGame != null)
                {
                    currentGame.Moves = moves.Trim();
                    games.Add(currentGame);
                    currentGame = null;
                    moves = string.Empty;
                }
                continue;
            }

            if (line.StartsWith("["))
            {
                if (currentGame == null) currentGame = new ChessGame();
                var match = Regex.Match(line, @"\[(\w+)\s+\""(.*?)\""\]");
                if (match.Success)
                {
                    string key = match.Groups[1].Value;
                    string value = match.Groups[2].Value;

                    switch (key)
                    {
                        case "Event": currentGame.Event = value; break;
                        case "Site": currentGame.Site = value; break;
                        case "Round": currentGame.Round = value; break;
                        case "White": currentGame.White = value; break;
                        case "Black": currentGame.Black = value; break;
                        case "WhiteElo": currentGame.WhiteElo = int.Parse(value); break;
                        case "BlackElo": currentGame.BlackElo = int.Parse(value); break;
                        case "Result":
                            currentGame.Result = value switch
                            {
                                "1-0" => "W",
                                "0-1" => "B",
                                "1/2-1/2" => "D",
                                _ => value
                            };
                            break;
                        case "EventDate":
                            currentGame.EventDate = DateTime.TryParse(value, out var date) ? date : new DateTime();
                            break;
                    }
                }
            }
            else
            {
                moves += line + " ";
            }
        }

        if (currentGame != null)
        {
            currentGame.Moves = moves.Trim();
            games.Add(currentGame);
        }

        return games;
    }
}
