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
            var user = await _context.Users
                .Include(u => u.MemberRank)
                .FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null) return;

            // Lấy rank phù hợp với tổng chi tiêu
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