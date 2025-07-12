using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Routing;
using VotingSystem.Data;
using static VotingSystem.Data.Db;

namespace VotingSystem
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ✅ Set connection string from appsettings.json
            Db.ConnectionString = builder.Configuration.GetConnectionString("Default");

            // ✅ Add routing support
            builder.Services.AddRouting();

            var app = builder.Build();

            // ✅ Serve static files (e.g., HTML from wwwroot)
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // ✅ Test SQL Server connection
            app.MapGet("/api/test", async () =>
            {
                var ok = await TestConnection();
                return ok ? Results.Ok("✅ SQL Server is connected!") : Results.Problem("❌ Connection failed.");
            });

            // ✅ Register a new user
            app.MapPost("/api/register", async (User user) =>
            {
                var message = await RegisterUser(user);
                return Results.Ok(message);
            });

            // ✅ Login existing user
            app.MapPost("/api/login", async (User login) =>
            {
                var user = await LoginUser(login.Email!, login.Password!);
                return user != null ? Results.Ok(user) : Results.Unauthorized();
            });

            // ✅ Apply as a candidate
            app.MapPost("/api/apply", async (ApplyRequest req) =>
            {
                var message = await ApplyAsCandidate(req.UserId, req.ElectionId);
                return Results.Ok(message);
            });

            // ✅ Get all elections
            app.MapGet("/api/elections", async () =>
            {
                var list = await GetElections();
                return Results.Ok(list);
            });

            // ✅ Submit vote
            app.MapPost("/api/vote", async (VoteRequest vote) =>
            {
                var result = await SubmitVote(vote.VoterId, vote.CandidateId, vote.ElectionId);
                return Results.Ok(result);
            });

            // ✅ Get candidates for a specific election
            app.MapGet("/api/candidates/{electionId:int}", async (int electionId) =>
            {
                var list = await GetCandidatesByElectionId(electionId);
                return Results.Ok(list);
            });

            // ✅ Get voting results for a specific election
            app.MapGet("/api/results/{electionId:int}", async (int electionId) =>
            {
                var results = await GetResultsByElectionId(electionId);
                return Results.Ok(results);
            });

            // ✅ Get vote count for specific candidate in an election
            app.MapGet("/api/votes/count", async (HttpRequest request) =>
            {
                if (!int.TryParse(request.Query["electionId"], out int electionId) ||
                    !int.TryParse(request.Query["candidateId"], out int candidateId))
                {
                    return Results.BadRequest("Missing or invalid parameters.");
                }

                int count = await VotingSystem.Data.Db.CountVotesForCandidate(electionId, candidateId);
                return Results.Ok(count);
            });

            // ✅ Show registered endpoints in console
            var endpointDataSource = app.Services.GetRequiredService<EndpointDataSource>();
            foreach (var endpoint in endpointDataSource.Endpoints)
            {
                Console.WriteLine("🔗 Registered: " + endpoint.DisplayName);
            }

            app.Run();
        }

        // ✅ Request body for candidate application
        public class ApplyRequest
        {
            public int UserId { get; set; }
            public int ElectionId { get; set; }
        }

        // ✅ Request body for submitting vote
        public class VoteRequest
        {
            public int VoterId { get; set; }
            public int CandidateId { get; set; }
            public int ElectionId { get; set; }
        }
    }
}
