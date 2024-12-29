using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;

namespace ShoeStore.Services
{
    public interface IMemberRankService
    {
        Task UpdateUserRank(int userId);
        Task<decimal> CalculateDiscountedTotal(decimal originalTotal, int userId);
    }

    public class MemberRankService : IMemberRankService
    {
        private readonly ApplicationDbContext _context;

        public MemberRankService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task UpdateUserRank(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.MemberRank)
                    .FirstOrDefaultAsync(u => u.UserID == userId);

                if (user == null)
                {
                    Console.WriteLine($"User {userId} not found");
                    return;
                }

                Console.WriteLine($"Updating rank for user {userId}");
                Console.WriteLine($"Current TotalSpent: {user.TotalSpent}");
                Console.WriteLine($"Current Rank: {user.MemberRank?.RankName ?? "None"}");

                var allRanks = await _context.MemberRanks
                    .OrderBy(r => r.MinimumSpent)
                    .ToListAsync();

                Console.WriteLine("Available ranks:");
                foreach (var rank in allRanks)
                {
                    Console.WriteLine($"- {rank.RankName}: Minimum spent {rank.MinimumSpent}");
                }

                var newRank = await _context.MemberRanks
                    .Where(r => r.MinimumSpent <= user.TotalSpent)
                    .OrderByDescending(r => r.MinimumSpent)
                    .FirstOrDefaultAsync();

                Console.WriteLine($"New rank found: {newRank?.RankName ?? "None"}");

                if (newRank != null && (user.MemberRankId != newRank.RankId))
                {
                    user.MemberRankId = newRank.RankId;
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Rank updated to: {newRank.RankName}");
                }
                else
                {
                    Console.WriteLine("No rank update needed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating rank: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<decimal> CalculateDiscountedTotal(decimal originalTotal, int userId)
        {
            var user = await _context.Users
                .Include(u => u.MemberRank)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user?.MemberRank == null) return originalTotal;

            var discountAmount = originalTotal * (user.MemberRank.DiscountPercent / 100m);
            return originalTotal - discountAmount;
        }
    }
} 