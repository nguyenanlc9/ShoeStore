using Microsoft.EntityFrameworkCore;
using ShoeStore.Models;

namespace ShoeStore.Services.MemberRanking
{
    public class MemberRankService : IMemberRankService
    {
        private readonly ApplicationDbContext _context;

        public MemberRankService(ApplicationDbContext context)
        {
            _context = context;
        }

        public MemberRank GetMemberRankBySpentAmount(decimal totalSpent)
        {
            return _context.MemberRanks
                .Where(r => r.MinimumSpent <= totalSpent)
                .OrderByDescending(r => r.MinimumSpent)
                .FirstOrDefault();
        }

        public async Task<MemberRank> GetMemberRankById(int id)
        {
            return await _context.MemberRanks.FindAsync(id);
        }

        public async Task<List<MemberRank>> GetAllMemberRanks()
        {
            return await _context.MemberRanks.OrderBy(r => r.MinimumSpent).ToListAsync();
        }

        public decimal CalculateDiscountAmount(decimal originalPrice, MemberRank rank)
        {
            if (rank == null) return 0;
            return originalPrice * rank.DiscountPercent / 100;
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
                    return;
                }

                var newRank = await _context.MemberRanks
                    .Where(r => r.MinimumSpent <= user.TotalSpent)
                    .OrderByDescending(r => r.MinimumSpent)
                    .FirstOrDefaultAsync();

                if (newRank != null && user.MemberRankId != newRank.RankId)
                {
                    user.MemberRankId = newRank.RankId;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error
                throw;
            }
        }
    }
}