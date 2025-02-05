using BadgerClan.Logic;
using BadgerClan.Logic.Bot;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string url = app.Configuration["ASPNETCORE_URLS"]?.Split(";").Last() ?? throw new Exception("Unable to find URL");
int port = new Uri(url).Port;

Console.Clear();
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Welcome to the Sample BadgerClan Bot!");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("The first time you run this program, please run the following two commands:");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\t winget install Microsoft.devtunnel");//DevTunnel explanation: https://learn.microsoft.com/en-us/azure/developer/dev-tunnels/overview
Console.WriteLine("\t devtunnel user login");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine();
Console.WriteLine("Change the code in Program.cs to add custom behavior.");
Console.WriteLine();
Console.WriteLine("Use the following URL to join your bot:");
Console.WriteLine();
Console.Write($"\tLocal:  ");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"{url}");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine();
Console.WriteLine("\tCompetition: 1) Start a DevTunnel for this port with the following command:");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\t                devtunnel host -p {port} --allow-anonymous");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine($"\t             2) Copy the \"Connect via browser\" URL from the DevTunnel output");
Console.WriteLine($"\t                (that will be your bot's URL)");
Console.WriteLine();
//Console.WriteLine("In the output from the 'devtunnel host' command, look for the \"Connect via browser:\" URL.  Paste that in the browser as your bot's address");


app.MapGet("/", () => "Sample BadgerClan bot.  Modify the code in Program.cs to change how the bot performs.");

app.MapPost("/", (GameState request) =>
{
    app.Logger.LogInformation("Received move request for game {gameId} turn {turnNumber}", request.GameId, request.TurnNumber);

    var myMoves = new List<Move>();
    // var myteam = state.TeamList.FirstOrDefault(t => t.Id == state.CurrentTeamId);

    var myTeam = new List<Unit>();
    var enemies = request.Units.Where(u => u.Team != request.YourTeamId);
    foreach (Unit unit in request.Units)
    {
        if (unit.Team == request.YourTeamId)
        {
            myTeam.Add(unit);
        }
    }


    foreach (Unit unit in myTeam.OrderByDescending(u => u.Type == "Knight"))
    {
        var closest = enemies.OrderBy(u => u.Location.Distance(unit.Location)).FirstOrDefault();
        var pointman = myTeam.OrderBy(u => u.Id).FirstOrDefault();
        Console.WriteLine($"Board:{request.BoardSize}, UnitR:{unit.Location.R}, UnitQ:{unit.Location.Q}");
        if (closest != null)
        {
            var closestTeam = enemies.Where(u => u.Team == closest.Team);
            Console.WriteLine(closestTeam.Count() + " Enemy Team Count");

            if (pointman != null && unit.Id != pointman.Id &&
                unit.Location.Distance(pointman.Location) > 5)
            {
                //Don't split up
                var toward = unit.Location.Toward(pointman.Location);
                myMoves.Add(new Move(MoveType.Walk, unit.Id, toward));
                myMoves.Add(new Move(MoveType.Walk, unit.Id, toward.Toward(pointman.Location)));
            }
            else if (myTeam.Count() <= closestTeam.Count())
            {
                myMoves.Add(StepAwayWithCircling(unit, closest.Location, request));
            }
            else if (unit.Type == "Archer" && closest.Location.Distance(unit.Location) == 1)
            {
                //Archers run away from knights
                var target = unit.Location.Away(closest.Location);
                myMoves.Add(new Move(MoveType.Walk, unit.Id, target));
                myMoves.Add(AttackClosest(unit, closest));
            }
            else if (closest.Location.Distance(unit.Location) <= 4)
            {
                //Console.WriteLine($"{unit.Health}:Health, {closest.Health}:EnemyHealth");
                myMoves.Add(StepToClosest(unit, closest, request));
                myMoves.Add(AttackClosest(unit, closest));
            }
            else if (request.Medpacs > 0 && unit.Health < unit.MaxHealth)
            {
                Console.WriteLine("Used MedPac");
                myMoves.Add(new Move(MoveType.Medpac, unit.Id, unit.Location));
            }
            else
            {
                myMoves.Add(StepToClosest(unit, closest, request));
                myMoves.Add(AttackClosest(unit, closest));
            }
        }
    }
    // ***************************************************************************
    // ***************************************************************************
    // **
    // ** Your code goes right here.
    // ** Look in the request object to see the game state.
    // ** Then add your moves to the myMoves list.
    // **
    // ***************************************************************************
    // ***************************************************************************


    return new MoveResponse(myMoves);
});

Move StepAway(Unit unit, Coordinate closest, GameState state)
{
    Random rnd = new Random();

    var target = unit.Location.Away(closest);

    var neighbors = unit.Location.Neighbors();

    while (state.Units.Any(u => u.Location == target) || IsAtOrNearWall(target, state))
    {
        if (neighbors.Any())
        {
            var i = rnd.Next(0, neighbors.Count() - 1);
            target = neighbors[i];
            neighbors.RemoveAt(i);
        }
        else
        {
            neighbors = unit.Location.MoveEast(1).Neighbors();
        }
    }

    var move = new Move(MoveType.Walk, unit.Id, target);
    return move;
}

Move StepToClosest(Unit unit, Unit closest, GameState state)
{
    Random rnd = new Random();

    var target = unit.Location.Toward(closest.Location);

    var neighbors = unit.Location.Neighbors();

    while (state.Units.Any(u => u.Location == target))
    {
        if (neighbors.Any())
        {
            var i = rnd.Next(0, neighbors.Count() - 1);
            target = neighbors[i];
            neighbors.RemoveAt(i);
        }
        else
        {
            neighbors = unit.Location.MoveEast(1).Neighbors();
        }
    }

    var move = new Move(MoveType.Walk, unit.Id, target);
    return move;
}

Move AttackClosest(Unit unit, Unit closest)
{
    var attack = new Move(MoveType.Attack, unit.Id, closest.Location);
    return attack;
}

bool IsAtOrNearWall(Coordinate coord, GameState state)
{
    int boardSize = state.BoardSize; // or request.BoardSize if available here
    // Adjust the numbers if you want a buffer
    return coord.Q <= 1 ||
           coord.R <= 1 ||
           coord.Q >= boardSize - 2 ||
           coord.R >= boardSize - 2;
}

// A helper method to rotate a vector (represented as a coordinate difference) by an angle in degrees.
Coordinate RotateVector(Coordinate vector, double degrees)
{
    // Convert degrees to radians.
    double radians = degrees * (Math.PI / 180);
    // Assuming vector components are (dx, dy). 
    // Adjust the math as needed if your Coordinate system differs.
    double cos = Math.Cos(radians);
    double sin = Math.Sin(radians);
    int newDx = (int)Math.Round(vector.Q * cos - vector.R * sin);
    int newDy = (int)Math.Round(vector.Q * sin + vector.R * cos);
    return new Coordinate(newDx, newDy);
}

Move StepAwayWithCircling(Unit unit, Coordinate threat, GameState state, int boardBuffer = 1)
{
    // First, compute the initial away vector.
    // Assume that a coordinate difference is defined as (unit - threat).
    var awayVector = new Coordinate(unit.Location.Q - threat.Q, unit.Location.R - threat.R);

    // Normalize the away vector to a step of one cell.
    // This normalization will depend on your Coordinate system. 
    // For a simple grid, you might do something like this:
    int stepQ = awayVector.Q != 0 ? awayVector.Q / Math.Abs(awayVector.Q) : 0;
    int stepR = awayVector.R != 0 ? awayVector.R / Math.Abs(awayVector.R) : 0;
    var stepVector = new Coordinate(stepQ, stepR);

    // Start with zero rotation (directly away).
    double rotationAngle = 0;
    double rotationStep = 15; // try rotating by 15 degrees increments
    bool foundTarget = false;
    Coordinate target = unit.Location.Away(threat); // fallback, if needed

    // Try up to, say, 360/15 * 2 times to try both clockwise and anticlockwise.
    for (int i = 0; i < 24; i++)
    {
        // Alternate the rotation direction (+ then -)
        double angle = (i % 2 == 0) ? rotationAngle : -rotationAngle;
        // Rotate the normalized vector.
        var rotated = RotateVector(stepVector, angle);

        // Determine the candidate target location.
        target = new Coordinate(unit.Location.Q + rotated.Q, unit.Location.R + rotated.R);

        // Check if the target is inside the board boundaries,
        // assuming the board is indexed from 0 to boardSize-1.
        if (target.Q < boardBuffer || target.R < boardBuffer ||
            target.Q >= state.BoardSize - boardBuffer || target.R >= state.BoardSize - boardBuffer)
        {
            // If it's too close to the wall, increment the rotation angle and try again.
            rotationAngle += rotationStep;
            continue;
        }

        // Check that the target is not occupied.
        if (state.Units.Any(u => u.Location.Equals(target)))
        {
            rotationAngle += rotationStep;
            continue;
        }

        // If both conditions pass, we found a valid target.
        foundTarget = true;
        break;
    }

    // If no good target was found, you might decide on a default behavior,
    // such as simply staying put or using the original direct-away target.
    if (!foundTarget)
    {
        target = unit.Location.Away(threat);
    }

    return new Move(MoveType.Walk, unit.Id, target);
}

app.Run();

public record GameState(IEnumerable<Unit> Units, IEnumerable<int> TeamIds, int YourTeamId, int TurnNumber, string GameId, int BoardSize, int Medpacs);
public record Unit(string Type, int Id, int Attack, int AttackDistance, int Health, int MaxHealth, double Moves, double MaxMoves, Coordinate Location, int Team);