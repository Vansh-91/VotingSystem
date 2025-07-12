using Microsoft.Data.SqlClient;
using Dapper;

namespace VotingSystem.Data
{
    public static class Db
    {
        public static string? ConnectionString { get; set; }

        // ✅ Test connection to SQL Server
        public static async Task<bool> TestConnection()
        {
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                await conn.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ✅ Register a new user
        public static async Task<string> RegisterUser(User user)
        {
            using var conn = new SqlConnection(ConnectionString);

            var exists = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT Email FROM Users WHERE Email = @Email",
                new { user.Email });

            if (exists != null)
                return "Email already exists";

            await conn.ExecuteAsync(
                "INSERT INTO Users (Name, Email, Password, Role) VALUES (@Name, @Email, @Password, 'user')",
                user
            );

            return "Registered successfully";
        }

        // ✅ Login a user
        public static async Task<User?> LoginUser(string email, string password)
        {
            using var conn = new SqlConnection(ConnectionString);

            return await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Email = @Email AND Password = @Password",
                new { Email = email, Password = password });
        }

        // ✅ Apply as a candidate for an election
        public static async Task<string> ApplyAsCandidate(int userId, int electionId)
        {
            using var conn = new SqlConnection(ConnectionString);

            var alreadyApplied = await conn.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(*) FROM Candidates WHERE UserId = @UserId AND ElectionId = @ElectionId",
                new { UserId = userId, ElectionId = electionId });

            if (alreadyApplied > 0)
                return "You have already applied for this election.";

            await conn.ExecuteAsync(
                "INSERT INTO Candidates (UserId, ElectionId) VALUES (@UserId, @ElectionId)",
                new { UserId = userId, ElectionId = electionId });

            return "Application submitted!";
        }

        // ✅ Get all elections
        public static async Task<IEnumerable<Election>> GetElections()
        {
            using var conn = new SqlConnection(ConnectionString);
            return await conn.QueryAsync<Election>("SELECT * FROM Elections");
        }

        // ✅ Submit Vote
        public static async Task<string> SubmitVote(int voterId, int candidateId, int electionId)
        {
            using var conn = new SqlConnection(ConnectionString);

            var voted = await conn.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(*) FROM Votes WHERE VoterId = @VoterId AND ElectionId = @ElectionId",
                new { VoterId = voterId, ElectionId = electionId });

            if (voted > 0)
                return "You have already voted in this election.";

            await conn.ExecuteAsync(
                "INSERT INTO Votes (VoterId, CandidateId, ElectionId) VALUES (@VoterId, @CandidateId, @ElectionId)",
                new { VoterId = voterId, CandidateId = candidateId, ElectionId = electionId });

            return "Vote submitted successfully!";
        }

        // ✅ Get all candidates for a specific election
        public static async Task<IEnumerable<CandidateDto>> GetCandidatesByElectionId(int electionId)
        {
            using var conn = new SqlConnection(ConnectionString);

            var sql = @"
                SELECT c.Id, u.Name AS CandidateName 
                FROM Candidates c
                JOIN Users u ON u.Id = c.UserId
                WHERE c.ElectionId = @ElectionId";

            return await conn.QueryAsync<CandidateDto>(sql, new { ElectionId = electionId });
        }

        // ✅ Get vote results for a specific election
        public static async Task<IEnumerable<ResultDto>> GetResultsByElectionId(int electionId)
        {
            using var conn = new SqlConnection(ConnectionString);
            var sql = @"
                SELECT u.Name AS CandidateName, COUNT(v.Id) AS VoteCount
                FROM Candidates c
                JOIN Users u ON u.Id = c.UserId
                LEFT JOIN Votes v ON v.CandidateId = c.Id AND v.ElectionId = @ElectionId
                WHERE c.ElectionId = @ElectionId
                GROUP BY u.Name
                ORDER BY VoteCount DESC";

            return await conn.QueryAsync<ResultDto>(sql, new { ElectionId = electionId });
        }

        // ✅ Count total votes for a specific candidate in an election
        public static async Task<int> CountVotesForCandidate(int electionId, int candidateId)
        {
            using var con = new SqlConnection(ConnectionString);
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM Votes WHERE ElectionId = @e AND CandidateId = @c", con);
            cmd.Parameters.AddWithValue("@e", electionId);
            cmd.Parameters.AddWithValue("@c", candidateId);
            await con.OpenAsync();
            return (int)(await cmd.ExecuteScalarAsync());
        }
    }

    // ✅ User model
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
    }

    // ✅ Election model
    public class Election
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    // ✅ DTO for displaying candidate info
    public class CandidateDto
    {
        public int Id { get; set; }
        public string CandidateName { get; set; }
    }

    // ✅ DTO for displaying election result info
    public class ResultDto
    {
        public string CandidateName { get; set; } = "";
        public int VoteCount { get; set; }
    }
}
