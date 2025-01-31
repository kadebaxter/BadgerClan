using BadgerClan.Logic;

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

    var myTeam = new List<UnitDto>();
    var enemies = request.Units.Where(u => u.Team != request.YourTeamId);
    foreach (UnitDto unit in request.Units)
    {
        if (unit.Team == request.YourTeamId)
        {
            myTeam.Add(unit);
        }
    }

    foreach (UnitDto unit in myTeam.OrderByDescending(u => u.Type == "Knight"))
    {
        var closest = enemies.OrderBy(u => u.Location.Distance(unit.Location)).FirstOrDefault();
        var pointman = myTeam.OrderBy(u => u.Id).FirstOrDefault();
        Console.WriteLine($"Board:{request.BoardSize}, UnitR:{unit.Location.R}, UnitQ:{unit.Location.Q}");
        if (closest != null)
        {
            if (pointman != null && unit.Id != pointman.Id &&
                unit.Location.Distance(pointman.Location) > 5)
            {
                //Don't split up
                var toward = unit.Location.Toward(pointman.Location);
                myMoves.Add(new Move(MoveType.Walk, unit.Id, toward));
                myMoves.Add(new Move(MoveType.Walk, unit.Id, toward.Toward(pointman.Location)));
            }
            //else if ()
            //{
            //    myMoves.Add(StepToClosest(unit, closest, request));
            //    myMoves.Add(AttackClosest(unit, closest));
            //}
            //Console.WriteLine($"{closest.Location.Distance(unit.Location)} vs. {closest.AttackDistance}");
            else if (enemies.Count() <= 10 || closest.Location.Distance(unit.Location) <= 4 || unit.Location.Q == 0 || unit.Location.R == 0 || unit.Location.R == request.BoardSize || unit.Location.Q == request.BoardSize)
            {
                //Console.WriteLine($"{unit.Health}:Health, {closest.Health}:EnemyHealth");
                myMoves.Add(StepToClosest(unit, closest, request));
                myMoves.Add(AttackClosest(unit, closest));
            }
            else if ( (closest.Location.Distance(unit.Location) <= 15) && (closest.Type == UnitType.Archer.ToString()) )
            {
                Console.WriteLine("Run From Archer " + closest.Location.Distance(unit.Location));
                myMoves.Add(StepAway(unit, closest.Location, request));
            }
            else if ( (closest.Location.Distance(unit.Location) <= 15) && (closest.Type == UnitType.Knight.ToString()))
            {
                Console.WriteLine("Run From Knight " + closest.Location.Distance(unit.Location));
                myMoves.Add(StepAway(unit, closest.Location, request));
            }        
            else if (request.Medpacs > 0 && unit.Health < unit.MaxHealth)
            {
                Console.WriteLine("Used MedPac");
                myMoves.Add(new Move(MoveType.Medpac, unit.Id, unit.Location));
            }
            
            //if (unit.Type == UnitType.Archer.ToString())
            //{

            //}
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

    var myMoves = SuperSimpleExampleBot.MakeMoves(request);//Very simple bot example.  Delete this line when you write your own bot.

    return new MoveResponse(myMoves);
});

Move StepAway(UnitDto unit, Coordinate closest, MoveRequest request)
{
    Random rnd = new Random();

    var target = unit.Location.Away(closest);

    var neighbors = unit.Location.Neighbors();

    while (request.Units.Any(u => u.Location == target))
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

Move StepToClosest(UnitDto unit, UnitDto closest, MoveRequest request)
{
    Random rnd = new Random();

    var target = unit.Location.Toward(closest.Location);

    var neighbors = unit.Location.Neighbors();

    while (request.Units.Any(u => u.Location == target))
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

Move AttackClosest(UnitDto unit, UnitDto closest)
{
    var attack = new Move(MoveType.Attack, unit.Id, closest.Location);
    return attack;
}

app.Run();

public record GameState(IEnumerable<Unit> Units, IEnumerable<int> TeamIds, int YourTeamId, int TurnNumber, string GameId, int BoardSize, int Medpacs);
public record Unit(string Type, int Id, int Attack, int AttackDistance, int Health, int MaxHealth, double Moves, double MaxMoves, Coordinate Location, int Team);