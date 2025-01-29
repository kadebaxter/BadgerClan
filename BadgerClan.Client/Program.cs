using BadgerClan.Logic;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string url = app.Configuration["ASPNETCORE_URLS"]?.Split(";").Last() ?? throw new Exception("Unable to find URL");
int port = new Uri(url).Port;

Console.Clear();
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Welcome to the Sample BadgerClan Bot!");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine("Change the code in Program.cs to add custom behavior.");
Console.WriteLine("If you're running this locally, use the following URL to join your bot:");
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine($"\t{url}");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine();
Console.WriteLine("For the competition, start a DevTunnel for this port with the following commands:");
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("\t winget install Microsoft.devtunnel");
Console.WriteLine("\t [ restart your command line after installing devtunnel ]");
Console.WriteLine("\t devtunnel user login");
Console.WriteLine($"\t devtunnel host -p {port} --allow-anonymous");
Console.ForegroundColor = ConsoleColor.White;
Console.WriteLine();
Console.WriteLine("In the output from the 'devtunnel host' command, look for the \"Connect via browser:\" URL.  Paste that in the browser as your bot's address");


app.MapGet("/", () => "Sample BadgerClan bot.  Modify the code in Program.cs to change how the bot performs.");

app.MapPost("/", (MoveRequest request) =>
{
    app.Logger.LogInformation("Received move request for game {gameId} turn {turnNumber}", request.GameId, request.TurnNumber);
    var currentTeam = new Team(request.YourTeamId)
    {
        Medpacs = request.Medpacs
    };
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

    foreach (UnitDto unit in myTeam)
    {
        var closest = enemies.OrderBy(u => u.Location.Distance(unit.Location)).FirstOrDefault();

        if (closest != null)
        {
            //Console.WriteLine($"{closest.Location.Distance(unit.Location)} vs. {closest.AttackDistance}");
            if (enemies.Count() <= 10 || closest.Location.Distance(unit.Location) <= 4)
            {
                Console.WriteLine($"{unit.Health}:Health, {closest.Health}:EnemyHealth");
                myMoves.Add(StepToClosest(unit, closest, request));
                myMoves.Add(AttackClosest(unit, closest));
            }
            else if ( (closest.Location.Distance(unit.Location) <= 18) && (closest.Type == UnitType.Archer.ToString()) )
            {
                Console.WriteLine("Run From Archer " + closest.Location.Distance(unit.Location));
                myMoves.Add(StepAway(unit, closest, request));
            }
            else if ( (closest.Location.Distance(unit.Location) <= 15) && (closest.Type == UnitType.Knight.ToString()) )
            {
                Console.WriteLine("Run From Knight " + closest.Location.Distance(unit.Location));
                myMoves.Add(StepAway(unit, closest, request));
            }
            else if (currentTeam.Medpacs > 0 && unit.Health < unit.MaxHealth)
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
    return new MoveResponse(myMoves);
});

Move StepAway(UnitDto unit, UnitDto closest, MoveRequest request)
{
    Random rnd = new Random();

    var target = unit.Location.Away(closest.Location);

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
